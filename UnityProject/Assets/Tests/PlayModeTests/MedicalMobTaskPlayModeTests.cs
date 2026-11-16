#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using Mirror;
using NUnit.Framework;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.TestTools;
using US13.Core.Physics;
using US13.Core.Utils;
using US13.HealthV2;
using US13.HealthV2.Living;
using US13.Mobs.BrainAI;
using US13.Mobs.BrainAI.ContextExtensions;
using US13.Mobs.BrainAI.Tasks;
using BodyPart = US13.HealthV2.Living.BodyParts.BodyPart;
using Object = UnityEngine.Object;

namespace Tests
{
	public class MedicalMobTaskPlayModeTests
	{
		private sealed class Fixture : IDisposable
		{
			public readonly GameObject Root = new("medical task test");
			public readonly BrainMobAI AI;
			public readonly MobBehaviorContext Context;
			public readonly TaskContextExtension Tasks;
			public readonly LivingHealthMasterBase Patient;
			public readonly BodyPart Part;
			public MobTaskExecution Execution;
			private readonly bool wasServer;
			private readonly TimeSample previousEarlySample, previousLateSample, previousFullSample;

			public Fixture(float duration = 0.05f)
			{
				Root.SetActive(false);
				wasServer = NetworkServer.active;
				previousEarlySample = NetworkServer.earlyUpdateDuration;
				previousLateSample = NetworkServer.lateUpdateDuration;
				previousFullSample = NetworkServer.fullUpdateDuration;
				NetworkServer.earlyUpdateDuration = new TimeSample(NetworkServer.sendRate);
				NetworkServer.lateUpdateDuration = new TimeSample(NetworkServer.sendRate);
				NetworkServer.fullUpdateDuration = new TimeSample(NetworkServer.sendRate);
				typeof(NetworkServer).GetProperty(nameof(NetworkServer.active)).SetValue(null, true);
				var body = Child("medbot"); body.AddComponent<NetworkIdentity>(); body.AddComponent<UniversalObjectPhysics>();
				AI = new BrainMobAI { Body = body.AddComponent<CommonComponents>(), ContextExtensions = new() { new TaskContextExtension() }, SettingsExtensions = new() { new BotTaskSettings() } };
				AI.GetSettings<BotTaskSettings>().Kind = MobTaskKind.Medical; AI.GetSettings<BotTaskSettings>().Duration = duration;
				Context = new MobBehaviorContext(AI);
				// Keep world-dependent initialization disabled while exercising the real health and async APIs.
				var patient = Child("patient"); patient.AddComponent<NetworkIdentity>(); patient.AddComponent<UniversalObjectPhysics>();
				Patient = patient.AddComponent<PlayerHealthV2>();
				typeof(LivingHealthMasterBase).GetField("healthStateController", BindingFlags.Instance | BindingFlags.NonPublic)
					.SetValue(Patient, patient.GetComponent<HealthStateController>());
				Part = Child("injured part").AddComponent<BodyPart>();
				Patient.BodyPartList.Add(Part); Patient.SurfaceBodyParts.Add(Part);
				Context.TryGetExtension(out Tasks); Tasks.Task = new MedicalMobTask();
				SelectPatient();
			}

			private GameObject Child(string name)
			{
				var child = new GameObject(name); child.transform.SetParent(Root.transform); return child;
			}
			public void SelectPatient() { Context.Target = Patient.gameObject; Tasks.TargetHealth = Patient; }
			public void Start() => Execution = new MobTaskExecution(Context, Time.time);
			public IEnumerator WaitFor(Func<bool> condition)
			{
				float deadline = Time.realtimeSinceStartup + 3;
				while (!condition())
				{
					Assert.That(Time.realtimeSinceStartup, Is.LessThan(deadline), "Medical task did not reach its expected state.");
					Execution.Tick(Time.time);
					yield return null;
				}
			}
			public void Dispose()
			{
				Execution?.Cancel();
				typeof(NetworkServer).GetProperty(nameof(NetworkServer.active)).SetValue(null, wasServer);
				NetworkServer.earlyUpdateDuration = previousEarlySample;
				NetworkServer.lateUpdateDuration = previousLateSample;
				NetworkServer.fullUpdateDuration = previousFullSample;
				Object.DestroyImmediate(Root);
			}
		}

		[UnityTest]
		public IEnumerator RepeatedTreatmentStaysRunningUntilAllTreatableDamageIsHealed()
		{
			using var fixture = new Fixture();
			fixture.Part.Damages[(int)DamageType.Brute] = 6;
			fixture.Part.Damages[(int)DamageType.Burn] = 4;
			fixture.Part.Damages[(int)DamageType.Tox] = 3;
			fixture.Start();
			yield return fixture.WaitFor(() => fixture.Patient.GetBruteBurnTotal() < 10);
			Assert.That(fixture.Patient.GetBruteBurnTotal(), Is.GreaterThan(0));
			Assert.That(fixture.Execution.Status, Is.EqualTo(Node.Status.Running));
			yield return fixture.WaitFor(() => fixture.Execution.Status != Node.Status.Running);
			Assert.That(fixture.Execution.Status, Is.EqualTo(Node.Status.Success));
			Assert.That(fixture.Patient.GetBruteBurnTotal(), Is.Zero);
			Assert.That(fixture.Part.Toxin, Is.EqualTo(3));
			Assert.That(fixture.Tasks.TargetHealth, Is.Null);
		}

		[UnityTest]
		public IEnumerator MovingAwayCompletesWithoutWaitingForTheNextDose()
		{
			using var fixture = new Fixture(10);
			fixture.Part.Damages[(int)DamageType.Brute] = 10;
			fixture.Start(); yield return null;
			fixture.Patient.transform.position = new Vector3(10, 0, 0);
			yield return fixture.WaitFor(() => fixture.Execution.Status != Node.Status.Running);
			Assert.That(fixture.Execution.Status, Is.EqualTo(Node.Status.Success));
			Assert.That(fixture.Part.Brute, Is.EqualTo(10));
			Assert.That(fixture.Tasks.TargetHealth, Is.Null);
		}

		[UnityTest]
		public IEnumerator FullRecoveryDuringTheWaitCompletesWithoutAnotherDose()
		{
			using var fixture = new Fixture(10);
			fixture.Part.Damages[(int)DamageType.Brute] = 10;
			fixture.Start(); yield return null;
			fixture.Part.Damages[(int)DamageType.Brute] = 0;
			yield return fixture.WaitFor(() => fixture.Execution.Status != Node.Status.Running);
			Assert.That(fixture.Execution.Status, Is.EqualTo(Node.Status.Success));
			Assert.That(fixture.Tasks.TargetHealth, Is.Null);
		}

		[UnityTest]
		public IEnumerator StoppedTreatmentCannotHealOrClearANewRunsPatient()
		{
			using var fixture = new Fixture(10);
			fixture.Part.Damages[(int)DamageType.Brute] = 10;
			fixture.Start(); var old = fixture.Execution;
			fixture.Context.EndRun(); fixture.Context.BeginRun();
			Assert.That(fixture.Tasks.TargetHealth, Is.Null);
			fixture.SelectPatient(); fixture.Start();
			old.Cancel();
			yield return null;
			Assert.That(fixture.Tasks.TargetHealth, Is.SameAs(fixture.Patient));
			Assert.That(fixture.Execution.Tick(Time.time), Is.EqualTo(Node.Status.Running));
			fixture.Execution.Cancel();
			yield return null;
			Assert.That(fixture.Tasks.TargetHealth, Is.Null);
			Assert.That(fixture.Part.Brute, Is.EqualTo(10));
		}

		[UnityTest]
		public IEnumerator RetargetingAbortsTreatmentAndReleasesTheCachedComponent()
		{
			using var fixture = new Fixture(10);
			fixture.Part.Damages[(int)DamageType.Brute] = 10;
			fixture.Start(); fixture.Context.Target = fixture.Root;
			yield return fixture.WaitFor(() => fixture.Execution.Status != Node.Status.Running);
			Assert.That(fixture.Execution.Status, Is.EqualTo(Node.Status.Failure));
			Assert.That(fixture.Tasks.TargetHealth, Is.Null);
			Assert.That(fixture.Part.Brute, Is.EqualTo(10));
		}

		[UnityTest]
		public IEnumerator DestroyingThePatientAbortsWithoutDereferencingItsHealthComponent()
		{
			using var fixture = new Fixture(10);
			fixture.Part.Damages[(int)DamageType.Brute] = 10;
			fixture.Start(); Object.DestroyImmediate(fixture.Patient.gameObject);
			yield return fixture.WaitFor(() => fixture.Execution.Status != Node.Status.Running);
			Assert.That(fixture.Execution.Status, Is.EqualTo(Node.Status.Failure));
			Assert.That(fixture.Tasks.TargetHealth, Is.Null);
			Assert.That(fixture.Part.Brute, Is.EqualTo(10));
		}

		[UnityTest]
		public IEnumerator DisablingTheAiStopsPendingTreatmentBeforeAnyHealing()
		{
			using var fixture = new Fixture(10);
			fixture.Part.Damages[(int)DamageType.Brute] = 10;
			fixture.Start(); fixture.AI.Enabled = false;
			yield return fixture.WaitFor(() => fixture.Execution.Status != Node.Status.Running);
			Assert.That(fixture.Execution.Status, Is.EqualTo(Node.Status.Failure));
			Assert.That(fixture.Tasks.TargetHealth, Is.Null);
			Assert.That(fixture.Part.Brute, Is.EqualTo(10));
		}
	}
}
#endif
