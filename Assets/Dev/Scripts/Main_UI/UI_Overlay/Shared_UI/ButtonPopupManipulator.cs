using UnityEngine.UIElements;

public class ButtonPopupManipulator : PointerManipulator
{
    private const string BoundClassName = "button-popup-manipulator-bound";
    private const float NormalYPixels = 0f;
    private const float HoverYPixels = -2f;
    private const float PressedYPixels = 1f;

    private bool _isHovering;
    private bool _isPressed;

    public static void ApplyTo(VisualElement element)
    {
        if (element == null || element.ClassListContains(BoundClassName))
            return;

        element.AddToClassList(BoundClassName);
        element.AddManipulator(new ButtonPopupManipulator());
    }

    protected override void RegisterCallbacksOnTarget()
    {
        target.RegisterCallback<PointerEnterEvent>(OnPointerEnter);
        target.RegisterCallback<PointerLeaveEvent>(OnPointerLeave);
        target.RegisterCallback<PointerDownEvent>(OnPointerDown);
        target.RegisterCallback<PointerUpEvent>(OnPointerUp);
        target.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
        target.RegisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    protected override void UnregisterCallbacksFromTarget()
    {
        target.UnregisterCallback<PointerEnterEvent>(OnPointerEnter);
        target.UnregisterCallback<PointerLeaveEvent>(OnPointerLeave);
        target.UnregisterCallback<PointerDownEvent>(OnPointerDown);
        target.UnregisterCallback<PointerUpEvent>(OnPointerUp);
        target.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
        target.UnregisterCallback<PointerCaptureOutEvent>(OnPointerCaptureOut);
    }

    private void OnPointerEnter(PointerEnterEvent evt)
    {
        _isHovering = true;

        if (!_isPressed)
            SetTranslateY(HoverYPixels);
    }

    private void OnPointerLeave(PointerLeaveEvent evt)
    {
        _isHovering = false;

        if (!_isPressed)
            SetTranslateY(NormalYPixels);
    }

    private void OnPointerDown(PointerDownEvent evt)
    {
        if (evt.button != 0)
            return;

        _isPressed = true;
        target.CapturePointer(evt.pointerId);
        SetTranslateY(PressedYPixels);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (evt.button != 0)
            return;

        _isPressed = false;

        if (target.HasPointerCapture(evt.pointerId))
            target.ReleasePointer(evt.pointerId);

        SetTranslateY(_isHovering ? HoverYPixels : NormalYPixels);
    }

    private void OnPointerCancel(PointerCancelEvent evt)
    {
        ResetTranslate();
    }

    private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
    {
        if (_isPressed)
            ResetTranslate();
    }

    private void ResetTranslate()
    {
        _isPressed = false;
        SetTranslateY(_isHovering ? HoverYPixels : NormalYPixels);
    }

    private void SetTranslateY(float yPixels)
    {
        target.style.translate = new Translate(
            new Length(0f, LengthUnit.Pixel),
            new Length(yPixels, LengthUnit.Pixel),
            0f);
    }
}
