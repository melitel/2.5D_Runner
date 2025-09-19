using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Crash Event Channel")]
public class CrashEventChannelSO : ScriptableObject
{
    public event Action<Vector3> OnEventRaised; // позиція вибуху
    public void Raise(Vector3 atPosition) => OnEventRaised?.Invoke(atPosition);
}