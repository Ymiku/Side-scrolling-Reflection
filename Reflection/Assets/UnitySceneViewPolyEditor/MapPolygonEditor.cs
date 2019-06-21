using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
[CustomEditor(typeof(MapPolygon))]
public class MapPolygonEditor : Editor {

	// Use this for initialization
	static Texture2D texMinus;
	static Texture2D texMinusSelected;
	static Texture2D texDot;
	static Texture2D texDotPlus;
	static Texture2D texDotSelected;


	private void CapDotMinus        (int aControlID, Vector3 aPosition, Quaternion aRotation, float aSize) {PolyEditor.EditorTools.ImageCapBase(aControlID, aPosition, aRotation, aSize, texMinus);}
	private void CapDotMinusSelected(int aControlID, Vector3 aPosition, Quaternion aRotation, float aSize) {PolyEditor.EditorTools.ImageCapBase(aControlID, aPosition, aRotation, aSize, texMinusSelected);}
	private void CapDot             (int aControlID, Vector3 aPosition, Quaternion aRotation, float aSize) {PolyEditor.EditorTools.ImageCapBase(aControlID, aPosition, aRotation, aSize, texDot);}
	private void CapDotPlus         (int aControlID, Vector3 aPosition, Quaternion aRotation, float aSize) {PolyEditor.EditorTools.ImageCapBase(aControlID, aPosition, aRotation, aSize, texDotPlus);}
	private void CapDotSelected     (int aControlID, Vector3 aPosition, Quaternion aRotation, float aSize) {PolyEditor.EditorTools.ImageCapBase(aControlID, aPosition, aRotation, aSize, texDotSelected);}


	bool       prevChanged    = false;
	List<int>  selectedPoints = new List<int>();
	bool       deleteSelected = false;
	Vector2    dragStart;
	bool       drag           = false;
	Vector3    snap           = Vector3.one;

	Vector3 _lastScale = Vector3.zero;
	Vector3 _lastPos = Vector3.zero;

	void LoadTextures() {
		if (texMinus != null) return;

		texMinus           = PolyEditor.EditorTools.GetGizmo("Assets/UnitySceneViewPolyEditor/Gizmos/dot-minus.png"         );
		texMinusSelected   = PolyEditor.EditorTools.GetGizmo("Assets/UnitySceneViewPolyEditor/Gizmos/dot-minus-selected.png");
		texDot             = PolyEditor.EditorTools.GetGizmo("Assets/UnitySceneViewPolyEditor/Gizmos/dot.png"               );
		texDotPlus         = PolyEditor.EditorTools.GetGizmo("Assets/UnitySceneViewPolyEditor/Gizmos/dot-plus.png"          );
		texDotSelected     = PolyEditor.EditorTools.GetGizmo("Assets/UnitySceneViewPolyEditor/Gizmos/dot-selected.png"      );
	}
	private         void OnEnable      () {
		MapPolygon  path      = (MapPolygon)target;
		if (path.GetVertsRaw ().Count == 0) {
			path.AddPointAt (0,Vector3.zero);
			path.AddPointAt (0,new Vector3(1f,0f,1f));
			path.AddPointAt (0,Vector3.right);
			path.Init ();
		}
		selectedPoints.Clear();
		LoadTextures();
	}
	private         void OnSceneGUI    () {
		MapPolygon  path      = (MapPolygon)target;
		GUIStyle     iconStyle = new GUIStyle();
		iconStyle.alignment    = TextAnchor.MiddleCenter;

		if (path.transform.position != _lastPos || path.transform.lossyScale != _lastScale) {
			path.ReCalculateWorldPoint ();
			_lastPos = path.transform.position;
			_lastScale = path.transform.lossyScale;
		}

		// setup undoing things
		Undo.RecordObject(target, "Modified Path");

		// draw the path line
		if (Event.current.type == EventType.Repaint)
			DoPath(path);
		
		// Check for drag-selecting multiple points
		//DragSelect(path);

		// draw and interact with all the path handles
		DoHandles (path, iconStyle);
		// update everything that relies on this path, if the GUI changed
		if (GUI.changed) {
			EditorUtility.SetDirty (target);
			prevChanged = true;
		} else if (Event.current.type == EventType.Used) {
			if (prevChanged == true) {
			}
			prevChanged = false;
		}
	}



	private void    DragSelect           (MapPolygon path) {
		List<Vector3> pathVerts = path.GetVertsRaw ();
		if (Event.current.type == EventType.Repaint) {
			if (drag) {
				Vector3 pt1 = HandleUtility.GUIPointToWorldRay(dragStart).GetPoint(0.2f);
				Vector3 pt2 = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition).GetPoint(0.2f);
				Vector3 pt3 = HandleUtility.GUIPointToWorldRay(new Vector2(dragStart.x, Event.current.mousePosition.y)).GetPoint(0.2f);
				Vector3 pt4 = HandleUtility.GUIPointToWorldRay(new Vector2(Event.current.mousePosition.x, dragStart.y)).GetPoint(0.2f);
				Handles.DrawSolidRectangleWithOutline(new Vector3[] { pt1, pt3, pt2, pt4 }, new Color(0, 0.5f, 0.25f, 0.25f), new Color(0, 0.5f, 0.25f, 0.5f));
			}
		}

		if (Event.current.shift && Event.current.control) {
			switch(Event.current.type) {
			case EventType.MouseDrag:
				SceneView.RepaintAll();
				break;
			case EventType.MouseMove:
				SceneView.RepaintAll();
				break;
			case EventType.MouseDown:
				if (Event.current.button != 0) break;

				dragStart = Event.current.mousePosition;
				drag      = true;

				break;
			case EventType.MouseUp:
				if (Event.current.button != 0)
					break;

				Vector2 dragEnd = Event.current.mousePosition;

				selectedPoints.Clear ();
				for	(int i=0;i<pathVerts.Count;i++) {
					float left   = Mathf.Min(dragStart.x, dragEnd.x);
					float right  = Mathf.Max(dragStart.x, dragEnd.x);
					float top    = Mathf.Min(dragStart.y, dragEnd.y);
					float bottom = Mathf.Max(dragStart.y, dragEnd.y);

					Rect r = new Rect(left, top, right-left, bottom-top);
					if (r.Contains(HandleUtility.WorldToGUIPoint(path.transform.TransformPoint( pathVerts[i]) ) )) {
						selectedPoints.Add(i);
					}
				}

				HandleUtility.AddDefaultControl(0);
				drag = false;
				SceneView.RepaintAll();
				break;
			case EventType.Layout :
				HandleUtility.AddDefaultControl(GetHashCode());
				break;
			}
		} else if (drag == true) {
			drag = false;
			Repaint();
		}
	}
	private void    DoHandles            (MapPolygon path, GUIStyle iconStyle)
	{
		Transform           transform    = path.transform;
		Matrix4x4           mat          = transform.localToWorldMatrix;
		Matrix4x4           invMat       = transform.worldToLocalMatrix;
		Transform           camTransform = SceneView.lastActiveSceneView.camera.transform;

		Handles.color = new Color(1, 1, 1, 1);
		for (int i = 0; i < path.GetVertsRaw().Count; i++)
		{
			// check if we want to remove points
			if (Event.current.alt) {
				DoResetModeHandles (path, i, mat, invMat, camTransform);
			} else {
				DoNormalModeHandles(path, i, mat, invMat, camTransform);
			}
		}

		if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete && selectedPoints.Count > 0) {
			deleteSelected = true;
			GUI.changed = true;
			Event.current.Use();
		}

		if (deleteSelected) {
			DeleteSelected(path);
			deleteSelected = false;
		}
	}

	private void DeleteSelected(MapPolygon path) {
		for (int i = 0; i < selectedPoints.Count; i++) {
			path.RemovePointAt(selectedPoints[i]);

			for (int u = 0; u < selectedPoints.Count; u++) {
				if (selectedPoints[u] > selectedPoints[i]) selectedPoints[u] -= 1;
			}
		}
		selectedPoints.Clear();
	}

	private void DoResetModeHandles(MapPolygon path, int i, Matrix4x4 mat, Matrix4x4 invMat, Transform camTransform) {
		List<Vector3> pathVerts = path.GetVertsRaw ();
		int     nextId     = i==pathVerts.Count-1?i%pathVerts.Count:i+1;
		Vector3 pos        = mat.MultiplyPoint3x4(pathVerts[i]);
		Vector3 posNext    = mat.MultiplyPoint3x4(pathVerts[nextId]);
		Vector3 posStart   = pos;
		bool    isSelected = false;
		if (selectedPoints!= null) isSelected = selectedPoints.Contains(i);

		float                   handleScale = HandleScale(posStart);
		Handles.DrawCapFunction cap         = (isSelected || selectedPoints.Count <= 0) ? (Handles.DrawCapFunction)CapDotMinusSelected : (Handles.DrawCapFunction)CapDotMinus;
		if (Handles.Button(posStart, camTransform.rotation, handleScale, handleScale, cap))
		{
			EnsureVertSelected(i, ref isSelected);
			deleteSelected = true;
			GUI.changed = true;
		} 
	}
	private void DoNormalModeHandles(MapPolygon path,int i, Matrix4x4 mat, Matrix4x4 invMat, Transform camTransform) {
		List<Vector3> pathVerts = path.GetVertsRaw ();
		int     nextId     = i==path.GetVertsRaw().Count-1?0:i+1;
		Vector3 pos        = mat.MultiplyPoint3x4(pathVerts[i]);
		Vector3 posNext    = mat.MultiplyPoint3x4(pathVerts[nextId]);
		bool    isSelected = false;
		if (selectedPoints != null) 
			isSelected = selectedPoints.Contains(i);

		// check for moving the point
		Handles.DrawCapFunction cap = CapDot;
		cap = isSelected ? (Handles.DrawCapFunction)CapDotSelected     : (Handles.DrawCapFunction)CapDot;

		Vector3 result = Handles.FreeMoveHandle(pos, Quaternion.Euler(Vector3.zero), HandleScale(pos), snap, cap);
		Handles.Label (pos,i.ToString(),EditorStyles.boldLabel);
		if (result != pos) {
			EnsureVertSelected(i, ref isSelected);

			for (int s = 0; s < selectedPoints.Count; s++) {
				path.UpdateVerts (selectedPoints [s],invMat.MultiplyPoint3x4 (result));
				//pathVerts [selectedPoints [s]] = invMat.MultiplyPoint3x4 (result);
			}
		}

		// make sure we can add new point at the midpoints!s
		Vector3 mid         = (pos + posNext) / 2f;
		float   handleScale = HandleScale(mid);
		if (Handles.Button(mid, camTransform.rotation, handleScale, handleScale, CapDotPlus)) {
			Vector3 pt = invMat.MultiplyPoint3x4(mid);
			path.AddPointAt(nextId,pt);
		}
	}

	private void EnsureVertSelected(int aIndex, ref bool aIsSelected) {
		if (selectedPoints.Count < 2 || aIsSelected == false) {
			selectedPoints.Clear();
			selectedPoints.Add(aIndex);
			aIsSelected = true;
		}
	}

	private void    DoPath               (MapPolygon path)
	{
		Handles.color = Color.white;
		List<Vector3> verts     = path.GetVertsRaw();
		Matrix4x4     mat       = path.transform.localToWorldMatrix;
		Vector3 pos;
		Vector3 pos2;
		for (int i = 0; i < verts.Count - 1; i++)
		{
			pos  = mat.MultiplyPoint3x4(verts[i]);
			pos2 = mat.MultiplyPoint3x4(verts[i + 1]);
			Handles.DrawLine(pos, pos2);
		}
		//连接第一个和最后一个
		pos  = mat.MultiplyPoint3x4(verts[0]);
		pos2 = mat.MultiplyPoint3x4(verts[verts.Count - 1]);
		Handles.DrawLine(pos, pos2);
	}

	public static Vector3 GetMousePos  (Vector2 aMousePos, Transform aTransform) {
		Ray   ray   = SceneView.lastActiveSceneView.camera.ScreenPointToRay(new Vector3(aMousePos.x, aMousePos.y, 0));
		Plane plane = new Plane(aTransform.TransformDirection(new Vector3(0,0,-1)), aTransform.position);
		float dist  = 0;
		Vector3 result = new Vector3(0,0,0);

		ray = HandleUtility.GUIPointToWorldRay(aMousePos);
		if (plane.Raycast(ray, out dist)) {
			result = ray.GetPoint(dist);
		}
		return result;
	}
	public static float   GetCameraDist(Vector3 aPt) {
		return Vector3.Distance(SceneView.lastActiveSceneView.camera.transform.position, aPt);
	}

    public static float HandleScale(Vector3 aPos)
    {
        float dist = SceneView.lastActiveSceneView.camera.orthographic ? SceneView.lastActiveSceneView.camera.orthographicSize / 0.45f : GetCameraDist(aPos);
        return Mathf.Min(0.4f, (dist / 5.0f) * 0.4f);
    }
    private static Vector3 GetRealPoint(Vector3 aPoint, Transform aTransform) {
		Plane p = new Plane( aTransform.TransformDirection(new Vector3(0, 0, -1)), aTransform.position);
		Ray   r = new Ray  (SceneView.lastActiveSceneView.camera.transform.position, aPoint - SceneView.lastActiveSceneView.camera.transform.position);
		float d = 0;

		if (p.Raycast(r, out d)) {
			return r.GetPoint(d);;
		}
		return aPoint;
	}
}
