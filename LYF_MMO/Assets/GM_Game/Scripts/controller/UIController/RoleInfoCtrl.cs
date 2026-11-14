using System;
using System.Collections.Generic;
using cfg;
using UnityEngine;

/// <summary>
/// 角色信息控制器，负责把角色和装备模型转换成界面展示数据。
/// </summary>
public sealed class RoleInfoCtrl : CtrlBase
{
    /// <summary>角色当前状态窗口。</summary>
    private readonly RoleCurrInfoWindow _roleCurrentWindow;
    /// <summary>角色装备属性窗口。</summary>
    private readonly RoleAttriibuteWindow _roleAttributeWindow;
    /// <summary>技能窗口，用于更新职业和点数标题。</summary>
    private readonly SkillInfoWindow _skillWindow;

    /// <summary>创建角色信息控制器并初始化当前角色显示。</summary>
    /// <param name="mainView">提供状态栏、装备窗口和技能标题的主界面。</param>
    public RoleInfoCtrl(MainView mainView) : base(mainView)
    {
        _roleCurrentWindow = mainView.RoleCurrentWindow;
        _roleAttributeWindow = mainView.RoleAttributeWindow;
        _skillWindow = mainView.SkillWindow;
        _roleAttributeWindow.GeneVisibilityChanged += OnGeneVisibilityChanged;
        RefreshCurrentRole();
    }

    /// <summary>当前本地角色的实际基因组件；窗口隐藏时仅保留绑定，不订阅变化。</summary>
    private MGeneComponent boundGene;
    /// <summary>当前是否已经订阅，确保显隐和角色替换不会重复注册。</summary>
    private bool geneSubscribed;

    /// <summary>角色替换时解除旧组件，打开的窗口立即显示新角色当前节点。</summary>
    /// <param name="role">当前本地角色，离场时为 null。</param>
    public void BindGeneRole(PlayerEntity role)
    {
        // 本地角色拥有的正式专用组件，未入场时为空。
        MGeneComponent next = null;
        if (role != null) next = role.Gene;
        if (boundGene == next) return;
        UnsubscribeGene();
        boundGene = next;
        if (_roleAttributeWindow.isActiveAndEnabled) OnGeneVisibilityChanged(true);
    }

    /// <summary>窗口打开读取当前状态，隐藏则解除组件事件。</summary>
    /// <param name="visible">窗口真实激活状态。</param>
    private void OnGeneVisibilityChanged(bool visible)
    {
        UnsubscribeGene();
        if (!visible) return;
        if (boundGene != null) { boundGene.Changed += RefreshGenes; geneSubscribed = true; }
        RefreshGenes();
    }

    /// <summary>只在已订阅时解绑旧组件，角色替换、隐藏和销毁共用。</summary>
    private void UnsubscribeGene()
    {
        if (geneSubscribed && boundGene != null) boundGene.Changed -= RefreshGenes;
        geneSubscribed = false;
    }

    /// <summary>列表变化或窗口重开时从实际节点生成中文展示行。</summary>
    private void RefreshGenes()
    {
        // 展示输入只存活于当前列表刷新，倒计时读取原节点。
        List<RoleGeneViewData> rows = new List<RoleGeneViewData>();
        if (boundGene != null)
        {
            // 当前根列表按服务器同步顺序显示，源 UID/槽区分同编号装备词条。
            for (int index = 0; index < boundGene.Runtime.ActiveGenes.Count; index++)
            {
                // 当前服务器确认的具体根。
                GeneBase gene = boundGene.Runtime.ActiveGenes[index];
                // 组合子树的事件与次数说明。
                List<string> details = new List<string>();
                CollectTriggerDetails(gene, details);
                if (details.Count == 0) details.Add(EffectLabel(gene.EffectType));
                rows.Add(new RoleGeneViewData(gene, gene.DisplayName, string.Join(" · ", details)));
            }
        }
        _roleAttributeWindow.RenderGenes(rows);
    }

    /// <summary>按真实子树显示事件和独立次数，不把根层数当成可触发次数。</summary>
    /// <param name="gene">当前待读取的具体节点。</param>
    /// <param name="details">当前行的中文触发说明列表。</param>
    private static void CollectTriggerDetails(GeneBase gene, List<string> details)
    {
        // 次数也是事件节点，先处理以避免重复显示一份时机。
        CountGene counted = gene as CountGene;
        if (counted != null) details.Add(EventLabel(counted.TriggerEvent) + " 剩余" + counted.RemainingTriggers + "次");
        else
        {
            // 普通事件显示触发时机，不显示虚构次数。
            EventGene triggered = gene as EventGene;
            if (triggered != null) details.Add(EventLabel(triggered.TriggerEvent) + "触发");
        }
        // 组合内嵌套的次数同样需要显示。
        for (int index = 0; index < gene.Children.Count; index++) CollectTriggerDetails(gene.Children[index], details);
    }

    /// <summary>将固定事件编号转换为界面说明。</summary>
    /// <param name="eventType">具体节点保存的真实事件。</param>
    /// <returns>简短中文时机。</returns>
    private static string EventLabel(GeneEventType eventType)
    {
        if (eventType == GeneEventType.AttackHit) return "命中";
        if (eventType == GeneEventType.BeHit) return "受击";
        if (eventType == GeneEventType.SkillCast) return "施法";
        if (eventType == GeneEventType.Kill) return "击败";
        return "事件";
    }

    /// <summary>没有事件信息时显示节点本身的效果类别。</summary>
    /// <param name="effect">具体节点分类。</param>
    /// <returns>界面使用的中文类别。</returns>
    private static string EffectLabel(GeneEffectType effect)
    {
        if (effect == GeneEffectType.Attribute) return "属性效果";
        if (effect == GeneEffectType.PeriodicDamage) return "持续伤害";
        if (effect == GeneEffectType.PeriodicHeal) return "持续治疗";
        if (effect == GeneEffectType.PeriodicMana) return "持续回蓝";
        if (effect == GeneEffectType.State) return "状态效果";
        return "组合效果";
    }

    /// <summary>主界面销毁时解除窗口和旧角色组件的全部新增订阅。</summary>
    public override void Dispose()
    {
        UnsubscribeGene();
        boundGene = null;
        _roleAttributeWindow.GeneVisibilityChanged -= OnGeneVisibilityChanged;
        base.Dispose();
    }

    /// <summary>从全局角色模型生成当前角色信息展示数据。</summary>
    public void RefreshCurrentRole()
    {
        if (_roleCurrentWindow == null || Global.Instance == null ||
            Global.Instance.SceneWorld == null)
        {
            return;
        }
        // 当前场景里的本地角色，信息窗口不持有跨角色缓存。
        PlayerEntity localRole = Global.Instance.SceneWorld.LocalRole;
        if (localRole == null)
        {
            return;
        }
        // 已装载的完整角色数据，生命、法力、职业和等级取 BaseInfo。
        BindGeneRole(localRole);
        MainRoleInfo roleInfo = localRole.RoleInfo;
        if (roleInfo == null || roleInfo.BaseInfo == null)
        {
            return;
        }

        // 职业名称优先取名称列表；未知编号直接显示其数字。
        string jobName = roleInfo.BaseInfo.JobId.ToString();
        if (roleInfo.BaseInfo.JobId >= 0 &&
            roleInfo.BaseInfo.JobId < ConstDefine.RoleJobName.Count)
        {
            jobName = ConstDefine.RoleJobName[roleInfo.BaseInfo.JobId];
        }
        // 当前仅为职业 1 配置头像路径，其他职业传空路径。
        string headIconPath = string.Empty;
        if (roleInfo.BaseInfo.JobId == 1)
        {
            headIconPath = "Icon/head_jianxiu";
        }

        // 生命条使用裁剪到 0～1 的比例。
        float hpRatio = CalculateRatio(roleInfo.BaseInfo.CurrHp, roleInfo.BaseInfo.MaxHp);
        // 法力条使用裁剪到 0～1 的比例。
        float mpRatio = CalculateRatio(roleInfo.BaseInfo.CurrMp, roleInfo.BaseInfo.MaxMp);
        // 生命正文同时显示当前值、上限和整数百分比。
        string hpText = roleInfo.BaseInfo.CurrHp + "/" + roleInfo.BaseInfo.MaxHp + " " +
            (hpRatio * 100f).ToString("F0") + "%";
        // 法力正文同时显示当前值、上限和整数百分比。
        string mpText = roleInfo.BaseInfo.CurrMp + "/" + roleInfo.BaseInfo.MaxMp + " " +
            (mpRatio * 100f).ToString("F0") + "%";
        _roleCurrentWindow.UpdateRoleInfo(new RoleCurrentViewData(roleInfo.BaseInfo.Nickname,
            jobName, roleInfo.BaseInfo.Level, headIconPath, hpRatio, mpRatio, hpText, mpText));
        if (_roleAttributeWindow != null)
        {
            // 服务器最终属性，各率字段仅显示协议已经明确的百分数。
            RoleBaseInfo info = roleInfo.BaseInfo;
            _roleAttributeWindow.RenderAttributes(new RoleAttributeViewData
            {
                Nickname = info.Nickname,
                JobName = jobName,
                HpText = hpText,
                MpText = mpText,
                HpRatio = hpRatio,
                MpRatio = mpRatio,
                AttributeValues = new string[]
                {
                    info.AtkExternalMin + "-" + info.AtkExternalMax,
                    info.AtkInternalMin + "-" + info.AtkInternalMax,
                    info.DefExternal.ToString(), info.DefInternal.ToString(),
                    info.HitPoint.ToString(), info.BlockPoint.ToString(),
                    info.ZhenJi.ToString(), info.WuGu.ToString(),
                    info.CritPoint.ToString(), info.CritResistPoint.ToString(),
                    info.CritDamage + "%", info.CritDamageReduce + "%",
                    info.HealStrength.ToString(), info.HealBonus + "%",
                    info.ZhuXin.ToString(), info.JianRen.ToString()
                },
                AdvancedValues = BuildAdvancedValues(info)
            });
        }
        if (_skillWindow != null)
        {
            // 武学面板只显示职业名，技能点已从界面移除。
            _skillWindow.SetHeader(jobName);
        }
    }

    /// <summary>按固定顺序把服务器高级属性转换成一位小数正文，未配置项显示 0.0。</summary>
    /// <param name="info">服务器下发的角色基础信息，包含已结算的高级属性。</param>
    /// <returns>与 RoleAttributeViewData.AdvancedEntries 同序的展示正文。</returns>
    private static string[] BuildAdvancedValues(RoleBaseInfo info)
    {
        // 高级属性静态定义表，长度即 44 项，顺序与协议编号一致。
        RoleAttributeViewData.AdvancedEntry[] entries = RoleAttributeViewData.AdvancedEntries;
        string[] values = new string[entries.Length];
        for (int index = 0; index < entries.Length; index++)
        {
            // 当前项的编号与展示单位。
            RoleAttributeViewData.AdvancedEntry entry = entries[index];
            // 面板只下发非零项，缺失按 0 处理。
            double value = 0d;
            info.AdvancedAttributes.TryGetValue(entry.Id, out value);
            if (entry.IsPercent)
            {
                values[index] = value.ToString("F1") + "%";
                continue;
            }
            values[index] = value.ToString("F1");
        }
        return values;
    }

    /// <summary>把当前穿戴背包转换成装备槽展示列表。</summary>
    /// <param name="knapsackInfo">服务端背包快照，只读取其中的当前穿戴装备列表。</param>
    public void RefreshEquipment(RoleKanpsackInfo knapsackInfo)
    {
        if (_roleAttributeWindow == null || knapsackInfo == null)
        {
            return;
        }

        // 只收录能确定装备部位的条目，交给窗口按部位显示。
        List<EquipmentViewData> equipments = new List<EquipmentViewData>();
        for (int index = 0; index < knapsackInfo.RoleCurrtEquipPack.Count; index++)
        {
            RoleItemInfo roleItem = knapsackInfo.RoleCurrtEquipPack[index];
            EquipmentViewData viewData;
            if (TryBuildEquipmentViewData(roleItem, out viewData))
            {
                equipments.Add(viewData);
            }
        }
        _roleAttributeWindow.ReFreshUI(equipments);
    }

    /// <summary>物品增量应用后直接读取穿戴容器刷新装备格，不使用登录时的旧快照。</summary>
    /// <param name="itemComponent">已经应用服务器完整装备数据的物品组件。</param>
    public void RefreshEquipment(RoItemComponent itemComponent)
    {
        if (_roleAttributeWindow == null) return;
        // 当前穿戴栏保存服务器确认的装备实例。
        RoItemContainer container = itemComponent.GetContainer(KnapsackType.RoleCurrtEquipPack);
        // 按索引读取原装备引用，构建纯展示列表。
        List<ItemBase> items = new List<ItemBase>(container.GetItemMap().Values);
        // 本次全量重绘的装备槽内容。
        List<EquipmentViewData> equipments = new List<EquipmentViewData>();
        for (int index = 0; index < items.Count; index++)
        {
            // 穿戴栏中的完整装备实例。
            EquipBase equip = items[index] as EquipBase;
            if (equip == null) continue;
            // 配置负责名称与图标，实例负责强化与唯一编号。
            EquipInfo config = LubanMgr.Instance.GetEquipInfoById(equip.GetItemID());
            if (config == null) continue;
            equipments.Add(new EquipmentViewData((EquipType)equip.EquipType,
                equip.GetItemID(), config.EquipName, config.Icon, equip.StrengthenLevel, equip.GetItemUID()));
        }
        _roleAttributeWindow.ReFreshUI(equipments);
    }

    /// <summary>计算并约束角色属性比例。</summary>
    /// <param name="currentValue">当前生命或法力。</param>
    /// <param name="maximumValue">对应上限；不为正时返回 0。</param>
    private static float CalculateRatio(float currentValue, float maximumValue)
    {
        if (maximumValue <= 0f)
        {
            return 0f;
        }
        return Mathf.Clamp01(currentValue / maximumValue);
    }

    /// <summary>将协议装备数据和 Luban 配置合成为装备展示数据。</summary>
    /// <param name="roleItem">一件当前穿戴装备的协议数据。</param>
    /// <param name="viewData">成功时返回装备部位、名称、图标和强化等级，失败为空。</param>
    private static bool TryBuildEquipmentViewData(RoleItemInfo roleItem,
        out EquipmentViewData viewData)
    {
        viewData = null;
        if (roleItem == null || roleItem.ItemTypeId <= 0)
        {
            return false;
        }

        // 配置提供显示名称、图标，以及协议未提供有效部位时的备用部位。
        EquipInfo equipConfig = LubanMgr.Instance.GetEquipInfoById(roleItem.ItemTypeId);
        // 最终使用的装备部位编号，优先采用协议中的有效枚举值。
        int equipTypeValue = 0;
        // 是否已从协议或配置中取得可识别的装备部位。
        bool hasValidEquipType = false;
        if (roleItem.EquipInfo != null &&
            Enum.IsDefined(typeof(EquipType), roleItem.EquipInfo.EquipType))
        {
            equipTypeValue = roleItem.EquipInfo.EquipType;
            hasValidEquipType = true;
        }
        if (!hasValidEquipType && equipConfig != null &&
            Enum.IsDefined(typeof(EquipType), equipConfig.EquipType))
        {
            equipTypeValue = equipConfig.EquipType;
            hasValidEquipType = true;
        }
        if (!hasValidEquipType)
        {
            return false;
        }

        // 配置缺失时使用物品编号作为显示名。
        string name = roleItem.ItemTypeId.ToString();
        // 配置缺失时按物品编号拼接历史图标路径。
        string iconPath = "Icon/Item/Item_" + roleItem.ItemTypeId;
        if (equipConfig != null)
        {
            name = equipConfig.EquipName;
            iconPath = equipConfig.Icon;
        }
        // 强化等级来自协议装备附加数据，未携带时显示 0。
        int strengthenLevel = 0;
        if (roleItem.EquipInfo != null)
        {
            strengthenLevel = roleItem.EquipInfo.StrengthenLevel;
        }
        viewData = new EquipmentViewData((EquipType)equipTypeValue, roleItem.ItemTypeId,
            name, iconPath, strengthenLevel, roleItem.ItemUid);
        return true;
    }
}
