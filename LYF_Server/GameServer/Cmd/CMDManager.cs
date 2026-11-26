using System;
using System.Collections.Generic;

/// <summary>按 RO 的 RunGMCommond、Process、FindProcessor 顺序分派游戏内 GM 命令。</summary>
internal sealed class CMDManager
{
    /// <summary>游戏服逻辑线程共用的已注册处理器。</summary>
    internal static readonly CMDManager Instance = new CMDManager();

    /// <summary>与 RO 相同，命令名大小写均可识别。</summary>
    private readonly Dictionary<string, CMDProcessor> processors = new Dictionary<string, CMDProcessor>(StringComparer.OrdinalIgnoreCase);

    /// <summary>进程启动时构建实际支持的处理器表。</summary>
    private CMDManager()
    {
        Register(new SimpleItemUpdateProcessor());
        Register(new AddBuffProcessor());
        Register(new DeleteBuffProcessor());
    }

    /// <summary>将单个处理器登记到它声明的命令名下。</summary>
    /// <param name="processor">游戏服内复用的无状态命令处理器。</param>
    private void Register(CMDProcessor processor)
    {
        processors.Add(processor.Name, processor);
    }

    /// <summary>先处理 RO 的内建命令，再查找按名称注册的处理器。</summary>
    /// <param name="role">游戏服已认证并完成数据装载的在线角色。</param><param name="cmd">面板输入的原始命令。</param>
    internal string RunGMCommond(OnlineRole role, string cmd)
    {
        // 连续空白仅作为分隔符，首词参与调度，其余词保留原文。
        string[] args = cmd.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
        if (args.Length == 0) return "请输入 GM 命令";
        switch (args[0].ToLowerInvariant())
        {
            case "item":
                if (args.Length != 4) return "用法：item add|remove <物品ID> <数量>";
                if (args[1].Equals("add", StringComparison.OrdinalIgnoreCase)) return ChangeItem(role, args, 2, false, false);
                if (args[1].Equals("remove", StringComparison.OrdinalIgnoreCase)) return ChangeItem(role, args, 2, true, false);
                return "用法：item add|remove <物品ID> <数量>";
            case "additem":
                return ChangeItem(role, args, 1, false, false);
            case "addmoney":
                return ChangeItem(role, args, 1, false, true);
            case "submoney":
                return ChangeItem(role, args, 1, true, true);
            case "baselevel":
            case "addexp":
                // 等级和经验由角色属性组件同步并随角色已有版本存档。
                int value;
                if (args.Length != 2 || !int.TryParse(args[1], out value)) return "用法：" + args[0] + " <整数>";
                if (args[0].Equals("baselevel", StringComparison.OrdinalIgnoreCase)) return role.AttrComp.SetLevel(value);
                return role.AttrComp.AddExp(value);
            case "help":
                return "addbuff / delbuff <基因ID>；item add / item remove <物品ID> <数量>；<物品ID> <正负数量>；addmoney / submoney <货币ID> <数量>；baselevel <等级>；addexp <经验>";
            default:
                return Process(role, args);
        }
    }

    /// <summary>读取物品或货币命令共用的两个参数。</summary>
    /// <param name="role">当前角色。</param><param name="args">完整命令词组。</param><param name="index">物品编号所在下标。</param>
    /// <param name="take">是否扣除。</param><param name="moneyOnly">是否限定为货币配置。</param>
    private static string ChangeItem(OnlineRole role, string[] args, int index, bool take, bool moneyOnly)
    {
        // 物品或虚拟货币在本项目表中的配置编号。
        int id;
        // 玩家要求变动的正数个数。
        int count;
        if (args.Length != index + 2 || !int.TryParse(args[index], out id) || !int.TryParse(args[index + 1], out count))
            return "用法：" + string.Join(" ", args, 0, index) + " <ID> <数量>";
        return SimpleItemUpdateProcessor.ChangeItem(role, id, count, take, moneyOnly);
    }

    /// <summary>数字首词发送给简写处理器，其余命令按名称查表。</summary>
    /// <param name="role">发起命令的角色。</param><param name="args">非空的命令词组。</param>
    private string Process(OnlineRole role, string[] args)
    {
        // 纯数字开头沿用 RO 的物品命令映射。
        string name = args[0];
        if (char.IsDigit(name[0])) name = "simpleitemupdate";
        // 仅执行游戏内已注册的处理器。
        CMDProcessor processor = FindProcessor(name);
        if (processor == null) return "未知 GM 命令：" + args[0] + "，输入 help 查看用法";
        return processor.Process(role, args);
    }

    /// <summary>取得命令名对应的处理器，未知命令交给面板提示。</summary>
    /// <param name="name">命令注册名。</param>
    private CMDProcessor FindProcessor(string name)
    {
        // 查到的原处理器可在不同玩家的游戏线程调用之间复用。
        processors.TryGetValue(name, out CMDProcessor processor);
        return processor;
    }
}
