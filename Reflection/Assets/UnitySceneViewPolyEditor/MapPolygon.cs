using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class MapPolygon:MonoBehaviour {
	[SerializeField,HideInInspector]
	private List<Vector3> _pivotsList = new List<Vector3>();
	[SerializeField,HideInInspector]
	private List<Vector3> _worldPivotsList = new List<Vector3>();
	public void Init()
	{
	}
	public void UpdateVerts(int index,Vector3 item)
	{
		_pivotsList [index] = item;
		_worldPivotsList [index] = transform.TransformPoint (item);
	}
	public List<Vector3> GetVertsRaw()
	{
		return _pivotsList;
	}
	public List<Vector3> GetWorldSpaceVertsRaw()
	{
		return _worldPivotsList;
	}

	public void AddPointAt(int index,Vector3 v)
	{
		_pivotsList.Insert (index,v);
		_worldPivotsList.Insert (index,transform.TransformPoint(v));
	}
	public void RemovePointAt(int index)
	{
		_pivotsList.RemoveAt (index);
		_worldPivotsList.RemoveAt (index);
	}
	public void RemovePoint(Vector3 v)
	{
		int index = _pivotsList.IndexOf (v);
		RemovePointAt (index);
	}
	public void ReCalculateWorldPoint()
	{
		for (int i = 0; i < _pivotsList.Count; i++) {
			_worldPivotsList [i] = transform.TransformPoint (_pivotsList[i]);
		}
	}
	void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		if (_worldPivotsList.Count <= 0)
			return;
		for (int i = 0; i < _worldPivotsList.Count-1; i++) {
			Gizmos.DrawLine (_worldPivotsList[i],_worldPivotsList[i+1]);
			Gizmos.DrawWireSphere (_worldPivotsList[i],0.2f);
		}
		if(IsClamp())
		Gizmos.DrawLine (_worldPivotsList[_worldPivotsList.Count-1],_worldPivotsList[0]);
		Gizmos.DrawWireSphere (_worldPivotsList[_worldPivotsList.Count-1],0.2f);
	}
	public virtual bool IsClamp()
	{
		return true;
	}
}
