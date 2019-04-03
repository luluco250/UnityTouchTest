using UnityEngine;

public class ObjectManipulator : MonoBehaviour {
	#region Parameter Variables

	[Tooltip(
		"How many pixels a touch must move before being considered a drag.\n" +
		"Any movement less than this will be considered a tap."
	)]
	public float dragStartThreshold = 10f;
	
	[Tooltip(
		"[UNIMPLEMENTED]\n" +
		"How many pixels both touches must move apart before affecting the " +
		"object scale."
	)]
	public float scaleStartThreshold = 10f;

	[Tooltip(
		"[UNIMPLEMENTED]\n" +
		"How many degrees both touches must move around before affecting the " +
		"object rotation."
	)]
	public float rotateStartThreshold = 10f;

	#endregion

	#region State Variables

	public enum State {
		None = 0,
		Tap = 1,
		Drag = 2,
		ScaleRotate = 3
	}

	[HideInInspector]
	public State state = State.None;
	[HideInInspector]
	public Manipulable target = null;

	struct SingleTouchProperties {
		// Start position of the touch in screen coordinates.
		public Vector2 start;
		// Offset at which the object was first touched in world coordinates.
		public Vector2 offset;
	}
	SingleTouchProperties singleProps;

	struct DualTouchProperties {
		public Vector2 start1, start2;
		public Vector2 pivot;
		public float startDistance;
		public Vector2 startTargetScale;
		public float startAngle;
		public float startTargetAngle;
	}

	DualTouchProperties dualProps;

	#endregion

	#region Gesture Logic Functions

	/*
		Performs the tap gesture logic.
	*/
	void TapLogic() {
		target.onTap.Invoke(target.gameObject);
	}
	
	/*
		Performs the drag gesture logic.

		Parameters:
			pos:
				Current touch position in world coordinates, aka
				Camera.main.ScreenToWorldPoint(Input.touches[0].position).
	*/
	void DragLogic(Vector2 pos) {
		var targetPos = target.transform.position;

		/*targetPos.x = pos.x + singleProps.offset.x;
		targetPos.y = pos.y + singleProps.offset.y;*/

		targetPos = pos + singleProps.offset;
		target.transform.position = targetPos;
	}

	/*
		Performs the scale and rotate gestures logic.

		Parameters:
			pos1:
				World position of the first touch.
			pos2:
				World position of the second touch.
			dist:
				Distance between the two touches in screen space.
			angle:
				Angle between the two touches.
	*/
	void ScaleRotateLogic(Vector2 pos1, Vector2 pos2, float dist, float angle) {
		// Change the scale.
		var targetScale = target.transform.localScale;
		float mult = dist / dualProps.startDistance;
		targetScale = dualProps.startTargetScale * mult;
		target.transform.localScale = targetScale;

		// Change the rotation.
		float targetRot = angle - dualProps.startAngle + dualProps.startTargetAngle;
		target.transform.localRotation = Quaternion.Euler(0f, 0f, targetRot);

		// Change the position.
		var targetPos = target.transform.position;
		var mPos = (pos1 + pos2) * 0.5f;
		targetPos = mPos + dualProps.pivot;
		target.transform.position = targetPos;
	}

	#endregion

	#region Touch Event Functions

	/*
		Called when a single touch is occuring.

		Parameters:
			touch:
				The current, single touch, aka Input.touches[0].
	*/
	void OnSingleTouch(Touch touch) {
		var touchPos = touch.position;

		if (state == State.Tap) {
			float dist = Vector2.Distance(touchPos, singleProps.start);

			if (dist > dragStartThreshold)
				state = State.Drag;

			return;
		}

		var pos = Camera.main.ScreenToWorldPoint(touchPos);
		
		if (state == State.Drag) {
			DragLogic(pos);
			return;
		}

		var hit = Physics2D.Raycast(pos, Vector2.zero);
		Manipulable mnp;

		if (mnp = GetManipulable(hit)) {
			target = mnp;
			state = State.Tap;
			singleProps.start = touchPos;
			singleProps.offset = target.transform.position - pos;
		}
	}

	/*
		Called when two touches are occurring.

		Parameters:
			touch1:
				The first current touch, aka Input.touches[0].
			touch2:
				The second current touch, aka Input.touches[1].
	*/
	void OnDualTouch(Touch touch1, Touch touch2) {
		var touch1Pos = touch1.position;
		var touch2Pos = touch2.position;

		float dist = Vector2.Distance(touch1Pos, touch2Pos);
		float angle = RotationBetween(touch1Pos, touch2Pos);

		var pos1 = Camera.main.ScreenToWorldPoint(touch1Pos);
		var pos2 = Camera.main.ScreenToWorldPoint(touch2Pos);

		if (state != State.ScaleRotate) {
			var hit1 = Physics2D.Raycast(pos1, Vector2.zero);
			var hit2 = Physics2D.Raycast(pos2, Vector2.zero);

			bool hasHit = false;
			Manipulable mnp;

			if (mnp = GetManipulable(hit1)) {
				target = mnp;
				hasHit = true;
			}

			if (mnp = GetManipulable(hit2)) {
				target = mnp;
				hasHit = true;
			}

			if (hasHit) {
				state = State.ScaleRotate;
				dualProps.start1 = touch1Pos;
				dualProps.start2 = touch2Pos;
				
				var offset1 = target.transform.position - pos1;
				var offset2 = target.transform.position - pos2;
				dualProps.pivot = (offset1 + offset2) * 0.5f;
				
				dualProps.startDistance = dist;
				dualProps.startTargetScale = target.transform.localScale;

				dualProps.startAngle = angle;
				dualProps.startTargetAngle = target.transform.rotation.eulerAngles.z;
			}

			return;
		}
		
		ScaleRotateLogic(pos1, pos2, dist, angle);
	}

	/*
		Called when all touches end.
	*/
	void OnNoTouch() {
		if (state == State.Tap)
			TapLogic();
		
		target = null;
		state = State.None;
	}
	
	void OnLeftMouse() {
		OnSingleTouch(new Touch{
			position = Input.mousePosition
		});
	}

	void OnRightMouse() {
		Vector2 pos1 = Input.mousePosition;
		Vector2? pos2;
		Vector2? tgtPos; // screen position of target

		if (target == null) {
			Vector2 worldPos1 = Camera.main.ScreenToWorldPoint(pos1);
			var hit = Physics2D.Raycast(worldPos1, Vector2.zero);

			if (!hit || !hit.transform.GetComponent<Manipulable>())
				return;
			
			tgtPos = Camera.main.WorldToScreenPoint(hit.transform.position);
		} else {
			tgtPos = Camera.main.WorldToScreenPoint(target.transform.position);
		}
	
		var diff = pos1 - tgtPos;
		pos2 = tgtPos - diff;

		OnDualTouch(
			new Touch{
				position = pos1
			}, new Touch{
				position = pos2.Value
			}
		);
	}

	#endregion

	#region Unity Functions

	void Update() {
		switch (Input.touchCount) {
		case 1:
			OnSingleTouch(Input.touches[0]);
			break;
		case 2:
			OnDualTouch(Input.touches[0], Input.touches[1]);
			break;
		case 0:
		default:
			if (Input.GetMouseButton(0))
				OnLeftMouse();
			else if (Input.GetMouseButton(1))
				OnRightMouse();
			else
				OnNoTouch();
			break;
		}
	}

	#endregion

	#region Helper Functions
	
	float RotationBetween(Vector2 a, Vector2 b) {
		var diff = a - b;
		return Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg + 180f;
	}

	Manipulable GetManipulable(RaycastHit2D hit) {
		return hit ? hit.transform.GetComponent<Manipulable>() : null;
	}

	#endregion
}
