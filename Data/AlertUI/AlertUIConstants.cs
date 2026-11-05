using UnityEngine;

namespace CrouchAlert.Data.AlertUI;

internal static class AlertUIConstants
{
    public static readonly AlertUITransform AboveTransform =
        new AlertUITransform(
            new Vector3(0f, 2.7f, 2.5f),
            new Vector3(-20f, 0f, 0f),
            1f
        );

    public static readonly AlertUITransform FloorTransform =
        new(
            new Vector3(0f, 0.01f, 3.0f),
            new Vector3(90f, 0f, 0f),
            3f
        );

    public static readonly Vector3 FloatingScreenScale = new Vector3(0.01f, 0.01f, 0.01f);
}