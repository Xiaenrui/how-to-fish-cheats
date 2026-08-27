// 渔力全开 (How to Fish) 作弊菜单解锁补丁
// 用法(先 Add-Type 加载本文件,Mono.Cecil 0.11.6 已在进程里):
//   [CecilPatch]::Run("完整路径\How to Fish_Data\Managed\Assembly-CSharp.dll")
// 自动把原文件备份成 .orig,然后在原位写回补丁后的程序集。

using System;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

public static class CecilPatch
{
    static MethodDefinition Find(AssemblyDefinition asm, string typeName, string methodName)
    {
        foreach (var t in asm.MainModule.Types)
        {
            if (t.Name != typeName) continue;
            foreach (var m in t.Methods)
                if (m.Name == methodName) return m;
        }
        return null;
    }

    static void ForceTrue(MethodDefinition m)
    {
        var il = m.Body.GetILProcessor();
        foreach (var i in new System.Collections.Generic.List<Instruction>(m.Body.Instructions))
            il.Remove(i);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Ret);
    }

    public static string Run(string dllPath)
    {
        string dir = Path.GetDirectoryName(dllPath);
        string backup = dllPath + ".orig";
        if (!File.Exists(backup)) File.Copy(dllPath, backup);

        var resolver = new DefaultAssemblyResolver();
        resolver.AddSearchDirectory(dir);
        var asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { AssemblyResolver = resolver, InMemory = true });
        var sb = new System.Text.StringBuilder();

        // 1) ClientSettings.get_CheatsEnabled -> true
        var g1 = Find(asm, "ClientSettings", "get_CheatsEnabled");
        if (g1 == null) return "ERROR: get_CheatsEnabled not found";
        ForceTrue(g1);
        sb.AppendLine("get_CheatsEnabled -> true");

        // 2) SteamManager.get_IsDev -> true
        var g2 = Find(asm, "SteamManager", "get_IsDev");
        if (g2 == null) return "ERROR: get_IsDev not found";
        ForceTrue(g2);
        sb.AppendLine("get_IsDev -> true");

        // 3) ButtonManager.Start: 强制显示 _cheatText
        MethodReference refGetGameObject = null, refSetActive = null, refGetWhite = null, refSetColor = null;
        foreach (var t in asm.MainModule.Types)
            foreach (var m in t.Methods)
            {
                if (!m.HasBody) continue;
                foreach (var i in m.Body.Instructions)
                {
                    var mr = i.Operand as MethodReference;
                    if (mr == null) continue;
                    string dc = mr.DeclaringType != null ? mr.DeclaringType.FullName : "";
                    if (mr.Name == "get_gameObject" && dc == "UnityEngine.Component" && refGetGameObject == null) refGetGameObject = mr;
                    if (mr.Name == "SetActive" && dc == "UnityEngine.GameObject" && refSetActive == null) refSetActive = mr;
                    if (mr.Name == "get_white" && dc == "UnityEngine.Color" && refGetWhite == null) refGetWhite = mr;
                    if (mr.Name == "set_color" && dc == "UnityEngine.UI.Graphic" && refSetColor == null) refSetColor = mr;
                }
            }
        if (refGetGameObject == null || refSetActive == null || refGetWhite == null || refSetColor == null)
            return "ERROR: missing UI method references";

        var start = Find(asm, "ButtonManager", "Start");
        if (start == null) return "ERROR: ButtonManager.Start not found";
        var cheatField = (FieldDefinition)null;
        foreach (var f in start.DeclaringType.Fields)
            if (f.Name == "_cheatText") cheatField = f;
        if (cheatField == null) return "ERROR: _cheatText field not found";

        var ilp = start.Body.GetILProcessor();
        var retIns = start.Body.Instructions[start.Body.Instructions.Count - 1];
        ilp.Remove(retIns);
        ilp.Append(Instruction.Create(OpCodes.Ldarg_0));
        ilp.Append(Instruction.Create(OpCodes.Ldfld, cheatField));
        ilp.Append(Instruction.Create(OpCodes.Callvirt, refGetGameObject));
        ilp.Append(Instruction.Create(OpCodes.Ldc_I4_1));
        ilp.Append(Instruction.Create(OpCodes.Callvirt, refSetActive));
        ilp.Append(Instruction.Create(OpCodes.Ldarg_0));
        ilp.Append(Instruction.Create(OpCodes.Ldfld, cheatField));
        ilp.Append(Instruction.Create(OpCodes.Call, refGetWhite));
        ilp.Append(Instruction.Create(OpCodes.Callvirt, refSetColor));
        ilp.Append(Instruction.Create(OpCodes.Ret));
        start.Body.MaxStackSize = 8;
        sb.AppendLine("ButtonManager.Start -> cheat text always visible");

        asm.Write(dllPath, new WriterParameters());
        sb.AppendLine("OK: patched -> " + dllPath + " (original backed up as .orig)");
        return sb.ToString();
    }
}
