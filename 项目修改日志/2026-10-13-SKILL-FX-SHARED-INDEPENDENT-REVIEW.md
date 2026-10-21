# 2026-10-13 技能特效共享依赖与修后画面独立审查

## 范围与修改文件

用户提供两个技能截图，要求重点检查 shared 打包。独立测试只读正式1071清单、临时修后43包、既有Shader替换实验和原生图像。本职责没有修改任何业务脚本、正式材质、贴图、ShaderGraph、场景、预制体、1071发布文件或收集规则。

新增临时测试产物位于 `_temp/skill-fx-appearance-20261013/review/`：`audit_after_independent.py`、`native-ipc-affected-root-check.cs.txt`、`make_named_skill_comparison.py`、独立机器报告 `independent-shared-review-final.json`、修后 `native-20261013102847/independent-after-audit.json` 与 `effective-metadata-differences.json`。详细专题引用 `项目分析/SKILL_FX_APPEARANCE_SHADER_VARIANTS.md`，主修复记录为 `项目修改日志/2026-10-13-SKILL-FX-APPEARANCE-CHANGELOG.md`。

## 实际问题与设计思路

- 正式1071的两个技能Prefab及3006001/3007001 Runtime均显式依赖BundleID47，即 `unityshaders.bundle`；HS_Blend_CG位于该包，清单哈希为26c970663cecd022fc412170c2186735、大小1321224字节，仍是旧Shader内容。当前证据不支持把材质或贴图颜色差异归为share_漏包。
- 已有43对材质贴图GPU回读RGBA逐像素相同、66材质1527项有效参数一致；仅替换包克隆HS_Blend_CG为源Shader即可消除白矩形和大部分黑轮廓。独立目检确认用户两截图形态与该实验吻合。
- 开发的三个最小资源修改纠正新变体保存目录与正式收集目录错位，保原meta/GUID及用户根集合；独立测试按相同相机、时间、固定粒子种子核前后画面，不只依引用非空或Shader支持标记通过。

## 修后验证

主统一Unity执行 `AllActualCollectionBoundary2`，真实完成标记在 `review/all-actual2-unity-result.json`；修后24对与48重复图位于 `review/native-20261013102847/`。独立执行 `analyze_captures.py` 与本轮 `audit_after_independent.py`。

- 24对中19对源包逐像素一致，48同源或同包重复全部一致。
- 用户点名的Effect_Skill3和Effect_Skill04各4个时间样例，共8对全部逐像素一致；修后源图与修前源图逐像素一致。已目检总览和两技能1.2秒图，白矩形、黑色粗大轮廓消失。
- 近图 `native-20261013102847/named-skills-before-after-source.png` 由现有图制成，列顺序为源、正式1071修前包、临时修后包；每行使用整组三图所有可见像素并集计算同一裁切边界与同一最近邻放大比例，未重新渲染、改色或只给一列换倍率。Skill3裁切[209,196,288,319]、Skill04裁切[199,115,303,381]，完整尾粒与白板均保留。
- 其它5对没有达到逐像素一致：Attack01的0.15秒差异72像素（阈值>5）、平均RGB差0.0035235；另外4对最大通道差均不超过5。五对可见轮廓IoU均1，不隐瞒此范围，不声明所有技能全时段全部一致。
- 元数据各2203条，71条初始差异全部是单色模式中不参与当前求值的colorMin/colorMax回读值。仅过滤这两字段后有效元数据差异0，Shader未支持记录0；该元数据结果仍不替代视觉验收。

## 失败记录与边界

前面after尝试使用YooAsset Simulate清单DependIDs选包。独立核得两Runtime在该模拟清单DependIDs均为空，但实际Runtime.bundle.manifest明确依赖五个FX包，实际Skill04另有share声音包；因此Particle null是测试加载筛选不完整，不能冒称生产1071漏shared。更正为按实际43包名加载后成功。此前测试同名包冲突与空粒子失败不计视觉通过。

修后构建存在 `Particles/Standard Surface` 的Shader Compiler IPC通信错误；实际43包读取成功和八对像素恢复不能把此错误改记为零错误。主已保存 `review/shader-build-ipc-evidence.txt`。主原生执行本职责探针成功，结果 `review/ipc-affected-root-native-result.txt`：四个Default-ParticleSystem根实际使用的是 `Particles/Standard Unlit`，而非此前猜测的Standard Surface；它们根emission均禁用且renderer均禁用，不能把Surface IPC误指为这些根的已确认故障。Skill02_Juqi根使用HS_Blend_CG但renderer禁用，Skill04_Juqi根使用Lightning并有burst，不笼统说六根发射都为零。

实包HS_Blend_CG的ShaderUtil消息为0；源HS_Blend_CG和Lightning各有三条宏重复定义Warning、无Error，源Standard Unlit消息0。临时unityshaders包枚举没有匹配Standard Surface，故此探针没有验收该IPC Shader；其历史编译失败仍保留为未完成边界。全文Editor.log关键词复核，本次渲染完成后没有新增IPC记录，不主动广泛重建。

独立重算冻结343条当前稿：342条保持，唯一变化为授权的正式MyShaderVariants.shadervariants（before b0ed22a6… → after 0d13d4b9…），缺失0。此冻结范围没有包含其余两授权资源，三资源全体由开发与主另核，不能称完整工作区保护。修后全部24张源图与修前源图逐像素一致，排除通过改变源Prefab/材质颜色让此组前后归因失效。证据 `review/independent-frozen-source-check.json`。

本轮正式1071输出没有重写或发布，没有安装包或正式主城联机视觉验收；结论只对已读取临时修后包与上述固定条件样例成立。下一步按修正后的原收集路径重新生成正式包并在两实际技能入口回归；临时证据保留，清理由产品经理确认。
