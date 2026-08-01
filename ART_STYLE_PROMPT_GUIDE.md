# 《冻线协议》战术像素美术提示词规范 V1.1

适用范围：角色、武器与道具、地图环境、游戏 UI。  
项目类型：顶视角搜打撤战术射击。  
核心目标：让不同批次、不同资产类别的生成结果保持同一套视觉语言，并能转化为实际游戏资产。

> 硬性约定：项目现有美术素材全部弃用。本规范不继承现有素材的造型、色彩、比例、分辨率或描边方式。四张外部参考图只用于分析像素组织、场景层次、空间表达和信息密度，不复制其中的角色、建筑、UI、标志或具体设计。

---

## 1. 一句话美术方向

**冷峻近未来战术生存 + 手工块面像素 + 90° 纯顶视正交空间 + 1 米/32 像素方格地图 + 克制低饱和主色 + 少量信号橙或冷青蓝功能点缀。**

画面应像一处真实运转过、后来失序的军事与民用交界区：装备务实，结构可信，磨损有原因，信息层级清楚；硬核但不写实摄影化，丰富但不杂乱，可爱化程度低。

## 2. 从参考图保留什么、修改什么

### 保留

- 清晰、统一的像素颗粒和手工 pixel cluster，而不是给高清插画套像素滤镜。
- 复杂场景由大块面、中型结构、小型叙事物件逐层组织。
- 正交俯视空间中清楚的分层、遮挡和轮廓组织方式；正式资产统一改为 90° 纯顶视，不继承参考图的斜俯视投影。
- 用有限色阶塑造材质，同一物体通常只用 3–4 个明度层级。
- 室内切面、遮挡关系、台阶、门窗、家具和地面边界表达明确。

### 修改

- 从温馨、明亮、童话和生活模拟气质，改成冷峻、实用、压迫感适中的战术生存气质。
- 降低整体饱和度；橙色和青蓝色优先承担阵营识别、交互提示、危险提示或电子设备状态，但允许美术为特定区域建立其他克制的功能色。
- 人物降低大头比例，避免 Q 版和玩具感；装备结构应可信，但不堆砌无法在游戏尺寸中辨认的零件。
- 建筑和道具从装饰性优先改为功能性优先：掩体、入口、撤离点、战利品点和危险区域一眼可辨。
- UI 从手账/童话装饰改成工业终端、军用标签、模块化仪表和磨损贴纸语言。

## 3. 不可变化的风格锁

以下英文段落是所有生成任务都要原样保留的 `GLOBAL_STYLE_LOCK_V1`。内容变量写在它前面，不要每次改写风格锁中的同义词。

```text
original 2D game art for a top-down extraction tactical shooter, grounded near-future military survival, cold restrained and hard-edged mood, hand-authored low-resolution pixel art, chunky deliberate pixel clusters, consistent pixel scale, crisp hard edges, no anti-aliasing, a cohesive limited palette chosen for the current asset set, restrained overall saturation, cool or neutral dominant hues with sparse functional accents such as oxidized orange or signal cyan, three-to-four-step value ramps, selective dark colored outlines instead of pure black, upper-left cool key light, short hard-edged contact shadows, worn functional materials, strong readable silhouettes, gameplay-first visual hierarchy, original design
```

### 3.1 固定视角

默认采用 **90° 纯顶视正交投影**：镜头垂直向下，只显示物体顶面、平面轮廓和必要的投影，不显示人物正脸、墙体立面、箱体正面或任何侧视面。地图由等大的正方格组成，世界坐标轴与画布水平/垂直轴平行。

```text
strict 90-degree overhead orthographic view, camera looking straight down, aligned to a square game grid, horizontal and vertical world axes parallel to the canvas edges, top surfaces and plan-view silhouettes only, no visible front faces or side faces, no horizon, no perspective convergence, no camera tilt
```

不要在世界资产中混入斜俯视、等距、侧视或带消失点的透视。库存图标和 UI 属于屏幕空间资产，可以为识别性选择独立展示角度，但不能被当作世界地图素材直接使用。

### 3.2 固定像素规格

| 资产 | 原生工作尺寸 | 建议有效占用 | 说明 |
|---|---:|---:|---|
| 地图基础方格 | 32×32 px | 满格 | 对应约 1×1 米世界空间，所有结构按此网格对齐 |
| 单格地面/墙体模块 | 32×32 px | 满格 | 四边可无缝拼接，墙体仅显示顶面占地 |
| 角色单方向单帧 | 48×48 px | 身体约 22–28 px 宽 | 角色占地约一格，手臂和枪械可以越出 32 px 占地框 |
| 小型世界道具 | 16×16 或 32×32 px | 按实际占地 | 与地图纯顶视视角一致 |
| 大型世界道具 | 32 px 网格整数倍 | 80%–100% | 明确占格、碰撞轮廓和交互边 |
| 背包/武器库存图标 | 64×64 px | 48–56 px | 不使用地图投影阴影 |
| 小型 UI 图标 | 16×16 / 24×24 / 32×32 px | 留 2–4 px 安全区 | 一个图标只表达一个意思 |

生成模型输出更高分辨率时，要求它模拟上述原生低分辨率画布；最终必须人工缩到原生尺寸清理像素。游戏中仅做 2×、3×、4×整数倍放大，使用 Point/Nearest 过滤。

### 3.3 柔性色彩框架

本项目**不使用全游戏统一的固定 HEX 色板，也不硬性限制为 24 色**。美术可以针对区域、天气、阵营、材质和情绪建立局部色板；稳定性来自明度结构、饱和度控制和功能色语义，而不是所有图片复用完全相同的颜色。

必须遵守：

- 整体保持偏冷、低到中等饱和度和克制的军事生存气质；避免全屏高饱和或糖果色。
- 单个物体以 3–4 阶明度塑造主要材质；大型场景可以拥有更多颜色，但相邻颜色必须形成清楚的色组，而不是连续渐变。
- 每个区域先确定一个主色氛围，再搭配一到两个辅助色组。海岸、工业区、地下设施、居民区和不同阵营可以拥有明显不同的局部色相。
- 橙色优先用于撤离、任务、警告和机械交互；青蓝优先用于电子信息、选中、能源和友方反馈。它们是语义建议，不要求使用固定色值。
- 强调色通常只占小面积，可按画面需求在约 3%–10% 之间调整；需要强烈危险或剧情时可以突破，但必须有明确目的。
- 最重要的是保持明度层级：可交互目标高于普通环境，角色高于地面，危险反馈高于装饰细节。

美术可以自由建立以下方向的局部色板：蓝灰与锈橙、灰绿与冷黄、海军蓝与信号青、灰褐与暗红等。只要整体冷峻、层级清楚、功能色语义稳定，就属于本规范允许的范围。

### 3.4 固定材质与光照语言

- 金属：以 3–4 阶冷色或中性色带塑造，边缘只做零星掉漆；不能全表面高光。
- 聚合物：大块哑光，反光比金属弱；避免光滑 3D 塑料感。
- 战术织物：脏橄榄或蓝灰，以口袋和绑带分块，不画细密编织纹。
- 混凝土：冷灰底、稀疏裂缝和污水痕；裂缝不能形成均匀噪点。
- 木材：灰褐、受潮、低饱和；只在临时加固和贫民设施中使用。
- 电子设备：暗色外壳，屏幕和状态灯用青蓝；警告与撤离相关设备用氧化橙。
- 主光固定从左上方照射；接触阴影向右下，边缘硬，长度短。
- 角色、拾取物和交互物的轮廓对比度高于地面约一个明度级；背景不能抢夺目标轮廓。

## 4. 通用提示词结构

每次按同一顺序填写。`[]` 是变量，生成前替换；没有的内容直接删除，不要把方括号留给模型。

```text
[ASSET TYPE AND SUBJECT]
[GAMEPLAY FUNCTION AND SILHOUETTE]
[VIEW / DIRECTION / POSE]
[MATERIALS AND WEAR]
[DOMINANT COLORS AND ONE ACCENT COLOR]
[LIGHTING AND SHADOW]
[CANVAS, BACKGROUND AND DELIVERY FORMAT]

[GLOBAL_STYLE_LOCK_V1]
[CATEGORY LOCK]
```

内容描述建议先写英文。中文可以用于内部需求表，但生产提示词尽量保持英文关键词和固定句序。

## 5. 通用负面提示词

支持 Negative Prompt 的模型直接填入；不支持时，将其改成末尾的 “Do not include …”。

```text
photorealistic, realistic painting, high-resolution digital illustration, 3D render, voxel art, vector art, smooth gradient, soft airbrush, anti-aliasing, blurry edges, subpixel texture, inconsistent pixel sizes, automatic pixelation filter, excessive dithering, pure black heavy outlines, glossy plastic, bloom, lens flare, depth of field, cinematic camera blur, strong perspective convergence, fisheye distortion, neon cyberpunk, colorful fantasy, pastel cozy style, cute chibi proportions, oversized head, toy-like weapons, ornamental fantasy armor, excessive saturation, random clutter, unreadable silhouette, text, letters, numbers, logo, watermark, signature, frame, mockup background
```

## 6. 角色生成模板

### 6.1 角色类别锁

```text
production-ready game character sprite, believable adult body mass interpreted from directly overhead and simplified for pixel readability, recognizable head, shoulder, forearm and weapon shapes from above, compact tactical silhouette, equipment grouped into clear large shapes, no visible face or frontal body planes, one character only, one direction only, one animation frame only, centered with a consistent torso-centered ground pivot
```

### 6.2 单角色单帧模板

```text
A [FACTION / ROLE] adult operator for a top-down extraction tactical shooter, wearing [HEADGEAR], [TORSO ARMOR], [BACKPACK OR RIG], [LEGWEAR] and [IDENTIFYING FEATURE], holding [WEAPON] in a safe low-ready combat stance. The overhead silhouette must instantly communicate [ROLE], with [PRIMARY SHAPE] as the main mass and [SECONDARY SHAPE] as the recognition cue. Facing [N / NE / E / SE / S / SW / W / NW]. Believable practical equipment, controlled wear on exposed edges, no decorative attachments without a gameplay purpose. Use [LOCAL PALETTE DESCRIPTION], with an optional [FUNCTIONAL ACCENT COLOR] on [SMALL FUNCTIONAL AREA]. Strict 90-degree overhead orthographic view, camera looking straight down, top surfaces and plan-view silhouette only, no visible face, front planes or side planes. 48 by 48 pixel native sprite canvas; keep the physical body footprint centered inside one 32 by 32 pixel one-meter tile, while arms and weapon may extend beyond that footprint. Transparent background, no ground plane, no baked shadow.

[GLOBAL_STYLE_LOCK_V1]
[CHARACTER CATEGORY LOCK]
```

### 6.3 可直接使用的角色示例

```text
An independent urban breacher adult operator for a top-down extraction tactical shooter, wearing a low-profile ballistic helmet, compact plate carrier, short assault backpack, reinforced field trousers and a single extraction tag on the right shoulder, holding a suppressed compact carbine in a cautious low-ready combat stance. The overhead silhouette must instantly communicate close-quarters breacher, with the broad rectangular shoulder-and-armor mass as the primary shape and the short breaching tool on the backpack as the recognition cue. Facing southeast. Believable practical equipment, controlled wear on knee pads and armor edges, no decorative attachments without a gameplay purpose. Use a restrained blue-charcoal, weathered steel and gray-green local palette, with muted oxidized orange limited to the shoulder tag and one magazine pull tab. Strict 90-degree overhead orthographic view, camera looking straight down, top surfaces and plan-view silhouette only, no visible face, front planes or side planes. 48 by 48 pixel native sprite canvas; keep the physical body footprint centered inside one 32 by 32 pixel one-meter tile, while arms and weapon may extend beyond that footprint. Transparent background, no ground plane, no baked shadow.

original 2D game art for a top-down extraction tactical shooter, grounded near-future military survival, cold restrained and hard-edged mood, hand-authored low-resolution pixel art, chunky deliberate pixel clusters, consistent pixel scale, crisp hard edges, no anti-aliasing, a cohesive limited palette chosen for the current asset set, restrained overall saturation, cool or neutral dominant hues with sparse functional accents such as oxidized orange or signal cyan, three-to-four-step value ramps, selective dark colored outlines instead of pure black, upper-left cool key light, short hard-edged contact shadows, worn functional materials, strong readable silhouettes, gameplay-first visual hierarchy, original design

production-ready game character sprite, believable adult body mass interpreted from directly overhead and simplified for pixel readability, recognizable head, shoulder, forearm and weapon shapes from above, compact tactical silhouette, equipment grouped into clear large shapes, no visible face or frontal body planes, one character only, one direction only, one animation frame only, centered with a consistent torso-centered ground pivot
```

### 6.4 角色一致性规则

- 先生成并确认 **东南方向待机帧**，它是角色母版。
- 其余 7 个方向必须把母版图作为角色参考，只改朝向，不重新描述人物。
- 动画一次只生成一个动作：待机、行走、奔跑、瞄准、换弹、受击、倒地分别处理。
- 不直接要求模型一次生成完整 8 方向 sprite sheet；这通常会造成装备、手势和像素密度漂移。
- 枪械方向、肩带、护肩、识别布必须在镜像方向中经过人工校正，不能机械翻转文字或非对称装备。

## 7. 道具、武器与战利品模板

世界道具与库存图标必须分开生成：世界道具有地图投影和接触阴影；库存图标强调识别，不带地图阴影。

### 7.1 世界道具类别锁

```text
production-ready world prop sprite, strict 90-degree overhead orthographic projection matching a square one-meter 32 by 32 pixel ground tile, top surfaces and plan-view silhouette only, no visible front faces or side faces, clear grid footprint and collision silhouette, functional construction, sparse purposeful wear, isolated object, no character, no scenery
```

### 7.2 世界道具模板

```text
A [PROP NAME] used as [GAMEPLAY FUNCTION], built from [MATERIAL 1] and [MATERIAL 2]. Its plan-view silhouette is [SIMPLE SHAPE DESCRIPTION], occupying exactly [WIDTH] by [DEPTH] square game tiles, each tile representing one meter at 32 by 32 pixels, with the interactive edge facing [DIRECTION]. Show [TWO OR THREE RECOGNITION DETAILS] on the top surface, but keep the object readable at game scale. Use [LOCAL PALETTE DESCRIPTION], with an optional [FUNCTIONAL ACCENT COLOR] restricted to [INTERACTIVE PART]. [CLEAN / USED / DAMAGED] condition with plausible wear. Strict 90-degree overhead orthographic view, camera looking straight down, aligned to the square grid, no visible front faces or side faces, upper-left cool key light, compact hard-edged contact shadow to the lower right. Transparent background, grid-aligned, no labels and no text.

[GLOBAL_STYLE_LOCK_V1]
[WORLD PROP CATEGORY LOCK]
```

### 7.3 库存图标类别锁

```text
production-ready square inventory icon sprite, one isolated item, icon presentation angle chosen for maximum recognition rather than world-space projection, strong outer silhouette, simplified internal details, centered, no perspective scene, no cast shadow outside the object, transparent background, 64 by 64 pixel native canvas, 4-pixel safe margin
```

### 7.4 库存图标模板

```text
Inventory icon of [ITEM], a [RARITY / CONDITION] [ITEM TYPE] used for [FUNCTION]. Show [DISTINCTIVE SHAPE OR COMPONENT] clearly. Practical construction in [MATERIALS], using [LOCAL PALETTE DESCRIPTION] and an optional small [FUNCTIONAL ACCENT COLOR] marker. Choose a clean icon presentation angle for maximum recognition; this is a screen-space icon and does not need to follow the world camera. Centered on a 64 by 64 pixel native canvas, transparent background, no text, no badge, no rarity frame, no loose parts.

[GLOBAL_STYLE_LOCK_V1]
[INVENTORY ICON CATEGORY LOCK]
```

### 7.5 可直接使用的道具示例

```text
Inventory icon of a portable encrypted field radio jammer, a high-value military electronic used to disable local surveillance. Show the thick rubber antenna, protected status screen and oversized side switch clearly. Practical construction in matte charcoal polymer, worn steel and gray-green webbing, with muted signal cyan restricted to the powered screen and one status LED. Choose a clean icon presentation angle for maximum recognition; this is a screen-space icon and does not need to follow the world camera. Centered on a 64 by 64 pixel native canvas, transparent background, no text, no badge, no rarity frame, no loose parts.

original 2D game art for a top-down extraction tactical shooter, grounded near-future military survival, cold restrained and hard-edged mood, hand-authored low-resolution pixel art, chunky deliberate pixel clusters, consistent pixel scale, crisp hard edges, no anti-aliasing, a cohesive limited palette chosen for the current asset set, restrained overall saturation, cool or neutral dominant hues with sparse functional accents such as oxidized orange or signal cyan, three-to-four-step value ramps, selective dark colored outlines instead of pure black, upper-left cool key light, short hard-edged contact shadows, worn functional materials, strong readable silhouettes, gameplay-first visual hierarchy, original design

production-ready square inventory icon sprite, one isolated item, icon presentation angle chosen for maximum recognition rather than world-space projection, strong outer silhouette, simplified internal details, centered, no perspective scene, no cast shadow outside the object, transparent background, 64 by 64 pixel native canvas, 4-pixel safe margin
```

## 8. 地图与环境模板

地图生成分为三类：整体氛围构图、可落地房间模块、可复用 tile/kit。三类不能混在一次提示中。

### 8.1 地图类别锁

```text
gameplay-first environment for a top-down extraction shooter, strict 90-degree overhead plan-view orthographic presentation, square one-meter grid at 32 by 32 pixels per tile, top surfaces and footprints only, no visible vertical facades, roofs removed over playable interiors, traversable floor remains visually quiet, cover and entrances have clear silhouettes, waist-high cover is distinct from full-height blockers through footprint design and value coding, grid-aligned architecture, controlled prop density, believable routes and tactical function
```

### 8.2 可落地房间/区域模块模板

```text
A grid-aligned [LOCATION TYPE] encounter module for a top-down extraction tactical shooter, measuring [WIDTH] by [HEIGHT] square tiles, with every one-meter tile represented by 32 by 32 pixels. The module contains [PRIMARY ROUTE], [SECONDARY ROUTE], [ONE FLANK OR SHORTCUT], [COVER TYPES], [LOOT LOCATION], [HAZARD] and [EXTRACTION / OBJECTIVE CUE IF ANY]. Architecture is [CONSTRUCTION TYPE] with [MATERIALS], showing believable use and abandonment. Keep at least 55 percent of traversable floor visually quiet. Use large value blocks and distinct footprint shapes to separate walkable floor, waist-high cover and full-height blockers. Place small props only along walls or inside intentional story clusters. Use [LOCAL PALETTE DESCRIPTION], with an optional [FUNCTIONAL ACCENT COLOR] reserved for [GAMEPLAY CUE]. Strict 90-degree overhead orthographic view, camera looking straight down, horizontal and vertical world axes parallel to the canvas edges, aligned to a square 32 by 32 pixel tile grid. Show top surfaces and plan-view footprints only; no visible wall facades, object front faces, object side faces, horizon, camera tilt or perspective convergence. Upper-left cool light with compact hard-edged shadows toward the lower right. No characters, no HUD, no labels, no text.

[GLOBAL_STYLE_LOCK_V1]
[MAP CATEGORY LOCK]
```

### 8.3 可直接使用的地图示例

```text
A grid-aligned abandoned coastal customs warehouse encounter module for a top-down extraction tactical shooter, measuring 14 by 10 square tiles, with every one-meter tile represented by 32 by 32 pixels. The module contains a wide central cargo lane, a narrow inspection-office route, one maintenance-door flank, concrete barriers and stacked shipping cases as cover, a locked evidence cage as the high-value loot location, a leaking chemical drum hazard and an orange emergency beacon marking the extraction control. Architecture is prefabricated concrete and corrugated steel with reinforced glass, showing believable salt corrosion, rushed evacuation and limited looting. Keep at least 55 percent of traversable floor visually quiet. Use large value blocks and distinct footprint shapes to separate walkable floor, waist-high cover and full-height blockers. Place small props only along walls or inside two intentional story clusters. Use a restrained storm-blue, wet concrete, gray-green and salt-stained local palette, with muted oxidized orange reserved for the extraction beacon, hazard strip and one case seal. Strict 90-degree overhead orthographic view, camera looking straight down, horizontal and vertical world axes parallel to the canvas edges, aligned to a square 32 by 32 pixel tile grid. Show top surfaces and plan-view footprints only; no visible wall facades, object front faces, object side faces, horizon, camera tilt or perspective convergence. Upper-left cool light with compact hard-edged shadows toward the lower right. No characters, no HUD, no labels, no text.

original 2D game art for a top-down extraction tactical shooter, grounded near-future military survival, cold restrained and hard-edged mood, hand-authored low-resolution pixel art, chunky deliberate pixel clusters, consistent pixel scale, crisp hard edges, no anti-aliasing, a cohesive limited palette chosen for the current asset set, restrained overall saturation, cool or neutral dominant hues with sparse functional accents such as oxidized orange or signal cyan, three-to-four-step value ramps, selective dark colored outlines instead of pure black, upper-left cool key light, short hard-edged contact shadows, worn functional materials, strong readable silhouettes, gameplay-first visual hierarchy, original design

gameplay-first environment for a top-down extraction shooter, strict 90-degree overhead plan-view orthographic presentation, square one-meter grid at 32 by 32 pixels per tile, top surfaces and footprints only, no visible vertical facades, roofs removed over playable interiors, traversable floor remains visually quiet, cover and entrances have clear silhouettes, waist-high cover is distinct from full-height blockers through footprint design and value coding, grid-aligned architecture, controlled prop density, believable routes and tactical function
```

### 8.4 Tile/Kit 模板

```text
A modular pixel-art environment kit for [LOCATION THEME], containing only [EXACT LIST OF PIECES]. Every piece uses a strict 90-degree overhead orthographic view and aligns to the same square one-meter 32 by 32 pixel ground grid, with seamless matching edges, coherent local color ramps and consistent upper-left lighting. Show top surfaces and plan-view footprints only, with no visible vertical facades. Pieces are separated with generous transparent spacing and never overlap. No assembled scene, no characters, no UI, no text, no decorative border.

[GLOBAL_STYLE_LOCK_V1]
```

生成 kit 时一次最多要求 6–8 个同类部件，例如只做“墙角与门框”或只做“地面破损变体”。不要一次要求完整关卡素材库。

## 9. UI 模板

### 9.1 UI 视觉语法

- 基础形状：矩形、切角矩形、窄标签、分段刻度、冲压孔、短连接线。
- 基础底色：优先选择低饱和暗色或中性色；具体色相可随阵营、地点和界面模块变化。主要文字区必须与底色形成稳定明度对比。
- 边框：原生尺寸 1–2 px；外深内浅，不做发光描边。
- 状态：同一套 UI 内必须建立并复用固定语义。推荐可交互/信息使用青蓝，撤离/任务使用橙，危险使用暗橙红，禁用使用低对比灰；具体色值由美术决定。
- 纹理：极少量划痕、贴纸残胶或扫描线，只能出现在大面板背景，不能穿过文字和图标。
- AI 不负责生成最终文字。要求空白文字槽，中文、数字和图标由游戏内字体/正式素材覆盖。

### 9.2 UI 类别锁

```text
production-ready pixel game UI, modular industrial tactical interface, rectangular panels with restrained clipped corners, 1 to 2 pixel borders at native resolution, flat value-separated surfaces, no gradients, no glow, strong information hierarchy, clean empty label zones for real game text, all decorative marks secondary to usability
```

### 9.3 单组件 UI 模板

```text
A pixel-art [UI COMPONENT] for [FUNCTION], sized [WIDTH] by [HEIGHT] pixels. Layout contains [INFORMATION ZONES] with a clear priority order of [PRIMARY], [SECONDARY], [TERTIARY]. Use [PANEL PALETTE DESCRIPTION] with clear dark, middle and light value roles, and reserve [FUNCTIONAL ACCENT COLOR] for [ACTIVE STATE]. Industrial tactical construction with restrained clipped corners, 1 to 2 pixel borders, integer-aligned spacing and an [8 / 4] pixel layout grid. Leave all text fields blank and clean for in-engine typography. Transparent background outside the component, front-facing flat UI asset, no scene, no device mockup.

[GLOBAL_STYLE_LOCK_V1]
[UI CATEGORY LOCK]
```

### 9.4 完整界面概念模板

```text
A complete [SCREEN NAME] interface concept for a top-down extraction tactical shooter at [TARGET ASPECT RATIO]. The screen includes [MODULE LIST]. The visual hierarchy prioritizes [PRIMARY PLAYER ACTION], then [SECONDARY INFORMATION], then [TERTIARY STATUS]. Use [PANEL PALETTE DESCRIPTION] with consistent dark, middle and light value roles; reserve [FUNCTIONAL ACCENT COLOR] for [EXACT STATE OR ACTION]. Inventory cells follow a strict grid and support 1 by 1, 1 by 2, 2 by 2 and larger item footprints. Leave every text area blank; represent text only as simple neutral placeholder bars. Flat front-facing UI, no perspective device, no characters, no background illustration.

[GLOBAL_STYLE_LOCK_V1]
[UI CATEGORY LOCK]
```

### 9.5 可直接使用的 UI 示例

```text
A complete raid inventory and loot interface concept for a top-down extraction tactical shooter at 16:9. The screen includes player equipment silhouette slots on the left, a strict backpack grid in the center, container loot grid on the right, weight and noise meters at the bottom, and compact health and currency status at the top. The visual hierarchy prioritizes moving and comparing loot, then equipment condition, then raid status. Use modular blue-charcoal panels, slate separators, pale gray content blocks and signal cyan for selected cells, with oxidized orange only on the leave-raid confirmation action. Inventory cells support 1 by 1, 1 by 2, 2 by 2 and larger item footprints. Leave every text area blank; represent text only as simple neutral placeholder bars. Flat front-facing UI, no perspective device, no characters, no background illustration.

original 2D game art for a top-down extraction tactical shooter, grounded near-future military survival, cold restrained and hard-edged mood, hand-authored low-resolution pixel art, chunky deliberate pixel clusters, consistent pixel scale, crisp hard edges, no anti-aliasing, a cohesive limited palette chosen for the current asset set, restrained overall saturation, cool or neutral dominant hues with sparse functional accents such as oxidized orange or signal cyan, three-to-four-step value ramps, selective dark colored outlines instead of pure black, upper-left cool key light, short hard-edged contact shadows, worn functional materials, strong readable silhouettes, gameplay-first visual hierarchy, original design

production-ready pixel game UI, modular industrial tactical interface, rectangular panels with restrained clipped corners, 1 to 2 pixel borders at native resolution, flat value-separated surfaces, no gradients, no glow, strong information hierarchy, clean empty label zones for real game text, all decorative marks secondary to usability
```

## 10. 建议的生产流程

1. **先做四件套基准板**：同一轮只生成 1 个角色、1 个战利品图标、1 个 8×8 房间模块、1 个库存面板。确认它们放在一起像同一个游戏。
2. **固定母版**：选定结果后保存为风格参考图；后续每次都同时提交母版图、当前区域或资产组的局部配色参考，以及本文件中的固定风格锁。
3. **一次只改一类变量**：角色批次只改职业/装备；道具批次只改物品；地图批次只改地点和路线；不要同时改视角、颜色和像素密度。
4. **一图一资产**：AI 生成阶段避免大而全的 sprite sheet；通过参考图逐方向、逐动作生成，再人工拼成正式 sheet。
5. **人工像素清理**：删除半透明边缘、渐变、孤立噪点和不规则像素；整理为连贯的局部色组和明度阶；检查 1× 尺寸可读性。
6. **游戏内验证**：在真实地面、真实缩放、暗区和战斗特效下检查轮廓；不要只在放大的透明背景上评价。
7. **版本化**：只有经过四件套基准验证后才能升级 `GLOBAL_STYLE_LOCK` 版本。已量产资产不要静默使用新版本。

## 11. 每批资产验收清单

### 通用

- [ ] 原生尺寸下像素格大小一致，没有高清线条和局部微像素。
- [ ] 没有抗锯齿、半透明描边、平滑渐变或摄影景深。
- [ ] 世界资产使用严格 90° 纯顶视，没有任何可见正面、侧面或镜头倾斜。
- [ ] 32×32 像素始终对应约 1×1 米，方格边缘和世界坐标轴平行。
- [ ] 使用左上主光和右下短接触影。
- [ ] 主体在 1× 尺寸下 1 秒内可辨认。
- [ ] 强调色面积克制且有明确功能；常规画面建议约 3%–10%，不是机械硬限制。
- [ ] 磨损集中在有接触、碰撞或暴露的位置，不是全图噪点。
- [ ] 没有文字、Logo、水印或对参考作品的具体设计复刻。

### 角色

- [ ] 身份、职业和武器类别可从轮廓识别。
- [ ] 纯顶视下仍能读出成年人的头、肩、手臂、武器和装备块面，不是圆头 Q 版。
- [ ] 八方向之间装备位置、手持关系和配色一致。
- [ ] 躯干中心枢轴、32×32 像素身体占地框、48×48 像素画布和枪口越界规则统一。

### 地图

- [ ] 可行走区、半身掩体、全高阻挡、入口和撤离提示可区分。
- [ ] 至少 55% 可行走地面保持低信息密度。
- [ ] 叙事杂物成组放置，不平均撒满地图。
- [ ] 墙角、门、台阶、掩体和交互边与 32×32 正方形网格一致。

### UI

- [ ] 空白文字区能容纳真实中文和数字，AI 占位符不会进入正式素材。
- [ ] 16×16 小图标仍能表达唯一含义。
- [ ] 橙色、青蓝色的状态语义全界面一致。
- [ ] 面板能拆为九宫格或模块化组件，而不是只能使用整张概念图。

## 12. 需要避免的提示词写法

不要只写：

```text
pixel art, tactical shooter, detailed, top down, like [某游戏]
```

原因是它没有锁定原生尺寸、投影、色板、光向、功能层级和交付形式，而且容易滑向直接模仿某个作品。

应写成：

```text
[明确资产与游戏功能] + [轮廓和视角] + [材质与磨损] + [主色与唯一强调色] + [原生画布和透明背景] + [GLOBAL_STYLE_LOCK_V1] + [CATEGORY LOCK]
```

这套顺序本身也是风格稳定机制的一部分。
