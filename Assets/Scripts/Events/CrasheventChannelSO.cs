using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Crash Event Channel")]
public class CrashEventChannelSO : ScriptableObject
{
    public event Action<Vector3> OnEventRaised; // dust cloud position
    public void Raise(Vector3 atPosition) => OnEventRaised?.Invoke(atPosition);
}