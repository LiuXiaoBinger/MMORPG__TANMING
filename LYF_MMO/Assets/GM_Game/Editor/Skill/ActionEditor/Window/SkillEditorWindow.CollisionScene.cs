// Scene 判定参考与手柄编辑，不运行正式物理或伤害结算。
using Ethan.ActionEditor;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

/// <summary>技能窗口的双语属性与原编辑操作适配，不改运行时或资产字段身份。</summary>
public partial class SkillEditorWindow
{
    /// <summary>显示活动或选中判定体，逐项访问原攻击对象引用。</summary>
    /// <param name="actor">预览角色根节点；为空时保留原世界坐标定位。</param>
    void DrawCollisionScene(Transform actor)
    {
        if (Event.current.type == EventType.MouseUp) _editGesture.Complete();
        // Unity 当前层级上下文对象或原判定选择项。
        var selected = SelectedCollision;
        // 遍历原可索引集合的零起始位置，仍访问原对象引用。
        for (int index = 0; index < atkList.Count; index++)
        {
            // 使用原攻击列表引用，选中几何编辑仍作用于原对象。
            Global.Attack collision = atkList[index];
            if (collision != null && (collision == selected || ActionCollisionGeometry.IsActive(collision, frameSelectIndex)))
                DrawCollisionVolume(collision, actor, collision == selected);
        }
        if (selType == SelType.Phase2Attack && selected != null) DrawCollisionVolume(selected, actor, true);
    }

    /// <summary>绘制原判定线框与编辑手柄，仅用户几何编辑才提交资产。</summary>
    /// <param name="collision">配置列表中的原攻击判定引用。</param>
    /// <param name="actor">预览角色根节点；为空时保留原世界坐标定位。</param>
    /// <param name="selected">该判定是否为窗口当前选择项。</param>
    void DrawCollisionVolume(Global.Attack collision, Transform actor, bool selected)
    {
        // 原判定几何的世界中心，单位为米。
        var center = ActionCollisionGeometry.Center(collision, actor, frameSelectIndex);
        // 原判定几何的世界旋转。
        var rotation = ActionCollisionGeometry.Rotation(collision, actor);
        // 保留球半径与盒全尺寸的几何定义。
        Vector3 size = ActionCollisionGeometry.BoxSize(collision);
        if (collision.shapeType == 0) size = Vector3.one * collision.parameter1;
        if (!ActionCollisionGeometry.IsFinite(center) || !ActionCollisionGeometry.IsFinite(size) || !ActionCollisionGeometry.IsFinite(collision.collisionRotation)) return;
        size = Vector3.Max(size, Vector3.one * .001f);
        // 当前播放头是否在原判定有效帧窗口内。
        bool active = ActionCollisionGeometry.IsActive(collision, frameSelectIndex);
        // 当前显示颜色，不改变任何技能数值。
        Color color = new Color(1f, .65f, .2f, .6f);
        if (selected) color = new Color(.3f, .85f, 1f, 1f);
        // 绘制前的 Handles 深度测试方式，结束时恢复。
        var previousDepth = Handles.zTest;
        try
        {
            Handles.zTest = UnityEngine.Rendering.CompareFunction.Always;
            using (new Handles.DrawingScope(color, Matrix4x4.TRS(center, rotation, Vector3.one)))
            {
                if (collision.shapeType == 0) {
                    Handles.DrawWireDisc(Vector3.zero, Vector3.up, size.x);
                    Handles.DrawWireDisc(Vector3.zero, Vector3.right, size.x);
                    Handles.DrawWireDisc(Vector3.zero, Vector3.forward, size.x);
                } else Handles.DrawWireCube(Vector3.zero, size);
            }
            using (new Handles.DrawingScope(color, Matrix4x4.identity))
            {
                // 标签高度保留原球半径／盒半高定位，单位为米。
                float labelHeight = size.y * .5f;
                if (collision.shapeType == 0) labelHeight = size.x;
                // 用户判定名称原样显示，空名称和窗口外状态才翻译。
                string collisionLabel = collision.collisionName;
                if (string.IsNullOrWhiteSpace(collisionLabel)) collisionLabel = SkillEditorLocalization.Text("Collision", "攻击判定");
                if (!active) collisionLabel += SkillEditorLocalization.Text(" (outside window)", "（窗口外）");
                Handles.Label(center + Vector3.up * labelHeight, collisionLabel);
                if (selected && editCollisionInScene && collisionPage == CollisionPage.Shape && !Ethan.ActionEditor.Editor.ActionConfigMigrationService.NeedsMigration(configFile))
                {
                    EditorGUI.BeginChangeCheck();
                    // 用户手柄修改后的世界中心，单位为米。
                    var newCenter = center; 
                    // 手柄编辑得到的世界朝向，不直接写源角色变换。
                    var newRotation = rotation;
                    if (collisionHandleMode == CollisionHandleMode.Move)
                    {
                        if (!collision.useBoneTracking || active) {
                            // 位置手柄保持原角色旋转空间，无角色时使用世界朝向。
                            Quaternion handleRotation = Quaternion.identity;
                            if (actor != null) handleRotation = actor.rotation;
                            newCenter = Handles.PositionHandle(center, handleRotation);
                        }
                    }
                    else if (collisionHandleMode == CollisionHandleMode.Rotate && collision.shapeType == 1)
                        newRotation = Handles.RotationHandle(rotation, center);
                    else if (collisionHandleMode == CollisionHandleMode.Resize)
                    {
                        using (new Handles.DrawingScope(color, Matrix4x4.TRS(center, rotation, Vector3.one)))
                        {
                            if (collision.shapeType == 0) {
                                sphereHandle.center = Vector3.zero; sphereHandle.radius = size.x;
                                sphereHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y | PrimitiveBoundsHandle.Axes.Z; sphereHandle.DrawHandle();
                                size = Vector3.one * sphereHandle.radius; newCenter += rotation * sphereHandle.center;
                            } else {
                                boxHandle.center = Vector3.zero; boxHandle.size = size;
                                boxHandle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y | PrimitiveBoundsHandle.Axes.Z; boxHandle.DrawHandle();
                                size = boxHandle.size; newCenter += rotation * boxHandle.center;
                            }
                        }
                    }
                    if (EditorGUI.EndChangeCheck())
                    {
                        // 将手柄结果还原到原角色局部空间，无角色时保留世界坐标。
                        Vector3 offset = newCenter;
                        // 相对角色根的判定四元数朝向，保存前转欧拉角。
                        Quaternion localRotation = newRotation;
                        if (actor != null) { offset = actor.InverseTransformPoint(newCenter); localRotation = Quaternion.Inverse(actor.rotation) * newRotation; }
                        ApplyCollisionGeometryEdit(collision, offset, size, localRotation.eulerAngles);
                    }
                    if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F)
                    { FrameSelectedCollision(); Event.current.Use(); }
                }
            }
        }
        finally { Handles.zTest = previousDepth; }
    }

    /// <summary>通过原编辑手势更新真实判定几何，保持局部空间与骨骼采样规则。</summary>
    /// <param name="collision">配置列表中的原攻击判定引用。</param>
    /// <param name="offset">判定局部偏移或无角色时世界偏移，单位为米。</param>
    /// <param name="size">球半径或盒完整尺寸，单位为米。</param>
    /// <param name="rotation">相对于角色的欧拉旋转，单位为度。</param>
    void ApplyCollisionGeometryEdit(Global.Attack collision, Vector3 offset, Vector3 size, Vector3 rotation)
    {
        if (configFile == null || Ethan.ActionEditor.Editor.ActionConfigMigrationService.NeedsMigration(configFile) || collision == null || !ActionCollisionGeometry.IsFinite(offset) || !ActionCollisionGeometry.IsFinite(size) || !ActionCollisionGeometry.IsFinite(rotation)) return;
        _editGesture.Record(configFile, SkillEditorLocalization.Text("Edit Collision Volume", "编辑攻击判定体"));
        collision.isReWrite = true;
        if (collision.useBoneTracking && collision.boneOffsets != null && collision.boneOffsets.Count > 0)
        {
            if (ActionCollisionGeometry.IsActive(collision, frameSelectIndex))
                collision.boneOffsets[Mathf.Clamp(frameSelectIndex - collision.keyNumber, 0, collision.boneOffsets.Count - 1)] = offset;
        }
        else collision.offset = offset;
        collision.parameter1 = Mathf.Max(.001f, size.x);
        if (collision.shapeType == 1) { collision.boxHeight = Mathf.Max(.001f, size.y); collision.parameter2 = Mathf.Max(.001f, size.z); collision.collisionRotation = rotation; }
        Ethan.ActionEditor.Editor.ActionEditorTransaction.MarkChanged(configFile);
        _serializedConfig?.Update(); RefreshValidation(); Repaint(); SceneView.RepaintAll();
    }
}
