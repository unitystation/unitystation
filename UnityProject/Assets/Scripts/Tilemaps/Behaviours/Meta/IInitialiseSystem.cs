using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IInitialiseSystem
{
	public int Priority { get; }
	public UniTask Initialize();
}
