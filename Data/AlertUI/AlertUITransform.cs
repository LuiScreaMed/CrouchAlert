using UnityEngine;

namespace CrouchAlert.Data.AlertUI;

public class AlertUITransform
{
    public Vector3 Position { get; set; }
    public Vector3 Rotation { get; set; }
    public float Scale { get; set; }

    public AlertUITransform(Vector3 position, Vector3 rotation, float scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    public AlertUITransform()
    {
        Position = new Vector3(0f, 2.7f, 2.5f);
        Rotation = new Vector3(-20f, 0f, 0f);
        Scale = 1f;
    }
}