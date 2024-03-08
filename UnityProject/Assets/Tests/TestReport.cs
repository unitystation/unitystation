using System;
using System.Collections.Generic;
using System.Text;
using Logs;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace Tests
{
	/// <summary>
	/// Designed for integration tests as currently we don't have access to Assert.Multiple. This handles multiple failure
	/// points in one test. The report can enter a permanent failure state if a condition or chain of conditions is met.
	/// The test will fail if the report is asserted and is in the failed state. This acts similar to a StringBuilder;
	/// however, the report can be dirtied to prevent appending after a failed condition. It can be cleaned to allow
	/// appending again but the report will remain in the failed state.
	/// </summary>
	public class TestReport
	{
		private StringBuilder Builder { get; }

		private bool Condition { get; set; }

		private bool Dirty { get; set; }

		private bool Failed { get; set; }

		public TestReport(StringBuilder builder = null) => Builder = builder ?? new StringBuilder();

		// Could use method group instead, but it creates garbage and this could be called a lot
		private TestReport TryAppend(Action<StringBuilder, string> action, string str)
		{
			if (Condition && Dirty == false) action?.Invoke(Builder, str);
			return this;
		}

		/// <summary>
		/// Appends a string to the report. Will only append if the previous condition failed and the report isn't dirty.
		/// <see cref="StringBuilder.Append(string)"/>
		/// </summary>
		public TestReport Append(string str) => TryAppend((b, s) => b.Append(s), str);

		/// <summary>
		/// Appends a line to the report. Will only append if the previous condition failed and the report isn't dirty.
		/// <see cref="StringBuilder.AppendLine(string)"/>
		/// </summary>
		public TestReport AppendLine(string line = null) => TryAppend((b, s) => b.AppendLine(s), line);

		/// <summary>
		/// Appends a sequence of lines to the report. Will only append if the previous condition failed and the report
		/// isn't dirty. <see cref="StringBuilder.AppendLine(string)"/>
		/// </summary>
		public TestReport AppendLineRange(IEnumerable<string> range, string prefix = "", string postfix = "")
		{
			if (Dirty || range is null) return this;

			foreach (var str in range)
			{
				AppendLine($"{prefix}{str}{postfix}");
			}

			return this;
		}

		/// <summary>
		/// Disallows further appending if the previous fail condition was met. Can be reset by cleaning the report.
		/// </summary>
		/// <seealso cref="Clean"/>
		public TestReport MarkDirtyIfFailed()
		{
			Dirty |= Condition;
			Condition = false;
			return this;
		}

		/// <summary>
		/// Fail this report regardless of conditions.
		/// </summary>
		/// <returns></returns>
		public TestReport Fail()
		{
			Condition = true;
			Failed = true;
			return this;
		}

		/// <summary>
		/// Leaves the report in a fail state for its lifetime if the condition is met.
		/// </summary>
		public TestReport FailIf(bool condition)
		{
			Condition = condition;
			if (condition) Failed = true;
			return this;
		}

		/// <summary>
		/// Leaves the report in a fail state for its lifetime if the condition meets the given Assert expression.
		/// </summary>
		public TestReport FailIf<T>(T obj, IResolveConstraint expression) =>
			FailIf(GetExpressionResult(obj, expression));

		/// <summary>
		/// Opposite version of FailIf. <see cref="FailIf"/>
		/// </summary>
		public TestReport FailIfNot(bool condition) => FailIf(condition == false);

		/// <summary>
		/// Opposite version of FailIf. <see cref="FailIf{T}"/>
		/// </summary>
		public TestReport FailIfNot<T>(T obj, IResolveConstraint expression) =>
			FailIf(GetExpressionResult(obj, expression) == false);

		private static bool GetExpressionResult<T>(T obj, IResolveConstraint expression) =>
			expression.Resolve().ApplyTo(obj).IsSuccess;

		/// <summary>
		/// Removes dirty status and allows appending again. This does not remove failure status.
		/// </summary>
		public TestReport Clean()
		{
			Dirty = false;
			return this;
		}

		public TestReport AssertPassed()
		{
			Assert.That(Failed, Is.False, Builder.ToString());
			return this;
		}

		/// <summary>
		/// Logs the report to the Tests category of the logger.
		/// </summary>
		public TestReport Log()
		{
			if (Failed && Builder.Length > 1) Loggy.Log(Builder.ToString(), Category.Tests);
			return this;
		}
	}

	public class TestReportTests
	{
		private TestReport Report { get; set; }

		[SetUp]
		public void SetupReport() => Report = new TestReport();

		[Test]
		public void CannotAddLineAfterMarkingDirty()
		{
			Report.Fail()
				.AppendLine("First")
				.MarkDirtyIfFailed()
				.AppendLine("Next");
			Assert.That(Report.AssertPassed, Throws.TypeOf<AssertionException>().With.Message.Not.Contains("Next"));
		}

		[Test]
		public void CanAddLineAfterFailingAndCleaning()
		{
			Report.Fail()
				.AppendLine("First")
				.MarkDirtyIfFailed()
				.Clean()
				.Fail()
				.AppendLine("Next");
			Assert.That(Report.AssertPassed, Throws.TypeOf<AssertionException>().With.Message.Contains("Next"));
		}

		[Test]
		public void ThrowsAfterFail() => AssertThrows(() => Report.Fail());

		[Test]
		public void ThrowsAfterFailIf() => AssertThrows(() => Report.FailIf(true));

		[Test]
		public void ThrowsAfterFailIfNot() => AssertThrows(() => Report.FailIfNot(false));

		[Test]
		public void ThrowsAfterFailIfConstrained() => AssertThrows(() => Report.FailIf(true, Is.True));

		[Test]
		public void ThrowsAfterFailIfNotConstrained() => AssertThrows(() => Report.FailIfNot(false, Is.True));

		private void AssertThrows(Func<TestReport> action) =>
			Assert.That(action().AssertPassed, Throws.TypeOf<AssertionException>());
	}
}