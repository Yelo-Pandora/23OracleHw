<template>
  <BoardLayout>
    <div class="toolbar">
      <div class="left">
        <label>楼层：</label>
        <button :class="{ active: currentFloor === -1 }" @click="setFloor(-1)">B1 停车场</button>
        <button :class="{ active: currentFloor === 1 }" @click="setFloor(1)">1F</button>
        <button :class="{ active: currentFloor === 2 }" @click="setFloor(2)">2F</button>
        <button :class="{ active: currentFloor === 3 }" @click="setFloor(3)">3F</button>
        <label style="margin-left:16px;">筛选：</label>
        <select v-model="layerFilter[currentFloor].type">
          <option value="">全部类型</option>
          <option value="shop">店铺</option>
          <option value="event">活动区</option>
          <option value="parking">车位</option>
          <option value="other">OTHERAREA</option>
        </select>
        <input v-model="layerFilter[currentFloor].keyword" placeholder="ID/名称 关键字" />
        <label style="margin-left:16px;">缩放：</label>
        <input type="range" min="0.6" max="2" step="0.1" v-model.number="layerZoom[currentFloor]" style="width:120px;" />
        <span>{{ currentZoom.toFixed(1) }}x</span>
      </div>
      <div class="right">
        <button :disabled="!canEdit" @click="onAddArea">新增区域</button>
        <button :disabled="!selectedId || !canEdit" @click="onDuplicate">复制</button>
        <button :disabled="!selectedId || !canEdit" @click="onDelete">删除</button>
        <button :disabled="!canEdit" @click="saveToLocal">保存</button>
        <button :disabled="!canEdit" @click="resetToDefault">重置示例</button>
        <button @click="exportJSON">导出JSON</button>
        <label class="import-btn">
          导入JSON
          <input type="file" accept="application/json" @change="importJSON">
        </label>
      </div>
    </div>

    <div class="canvas-wrap" @mousemove="onMouseMove" @mouseleave="hovered = null">
      <svg :viewBox="`0 0 ${canvasSize.w} ${canvasSize.h}`" preserveAspectRatio="xMidYMid meet">
        <defs>
          <marker id="arrow" markerWidth="10" markerHeight="10" refX="8" refY="3" orient="auto-start-reverse">
            <path d="M0,0 L0,6 L9,3 z" fill="#666" />
          </marker>
        </defs>
        <rect :width="canvasSize.w" :height="canvasSize.h" fill="#f7f7f7" stroke="#666" stroke-width="2" />

        <!-- 走道/过道：外围虚线框 + 网格线，清晰区分可通行区域 -->
        <g class="walkways">
          <rect :x="walk.inner.x" :y="walk.inner.y" :width="walk.inner.w" :height="walk.inner.h" fill="none" stroke="#bbb" stroke-width="1.5" stroke-dasharray="6 6" />
          <line v-for="(x,i) in walk.vertical" :key="'wv-'+i" :x1="x" :y1="walk.inner.y" :x2="x" :y2="walk.inner.y + walk.inner.h" stroke="#bbb" stroke-dasharray="8 8" />
          <line v-for="(y,i) in walk.horizontal" :key="'wh-'+i" :x1="walk.inner.x" :y1="y" :x2="walk.inner.x + walk.inner.w" :y2="y" stroke="#bbb" stroke-dasharray="8 8" />
        </g>

        <!-- 内容整体自适应居中（仅平移） -->
        <g :transform="transformFit">

        <!-- 只绘制当前楼层的区域（不含停车位） -->
        <g v-for="a in visibleAreas" :key="a.id">
          <template v-if="a.points && a.points.length">
            <polygon
              :points="pointsToString(a.points)"
              :fill="areaFill(a)"
              :stroke="selectedId === a.id ? '#ff7f0e' : '#333'"
              :stroke-width="selectedId === a.id ? 3 : 1"
              @click.stop="select(a.id)"
              @mousedown.stop.prevent="canEdit && startDrag(a, $event)"
              @mouseenter="hovered = { id: a.id, x: polyBounds(a).x2 + 6, y: polyBounds(a).y1 }"
            />
          </template>
          <template v-else>
            <rect
              :x="a.x"
              :y="a.y"
              :width="a.w"
              :height="a.h"
              :fill="areaFill(a)"
              :stroke="selectedId === a.id ? '#ff7f0e' : '#333'"
              stroke-width="selectedId === a.id ? 3 : 1"
              @click.stop="select(a.id)"
              @mousedown.stop.prevent="canEdit && startDrag(a, $event)"
              @mouseenter="hovered = { id: a.id, x: a.x + a.w + 6, y: a.y }"
            />
          </template>
          <text class="label" :x="labelPos(a).x" :y="labelPos(a).y" text-anchor="middle" dominant-baseline="middle">{{ a.id }}</text>
        </g>

        <!-- B1 停车位，仅在 B1 展示，可点切换占用状态 -->
        <g v-if="currentFloor === -1">
          <g v-for="p in visibleParking" :key="p.id">
            <polygon
              :points="pointsToString([[p.x, p.y],[p.x+p.w, p.y+p.skew],[p.x+p.w, p.y+p.h+p.skew],[p.x, p.y+p.h]])"
              :fill="p.occupied ? '#d9534f' : '#5cb85c'"
              stroke="#222" stroke-width="1"
              @click.stop="togglePark(p)"
              @mouseenter="hovered = { id: p.id, x: p.x + p.w + 6, y: p.y }"
            />
            <text :x="p.x + p.w/2" :y="p.y + p.h/2" text-anchor="middle" dominant-baseline="middle" fill="#fff" font-size="10">{{ p.no }}</text>
          </g>
          <!-- 车道辅助线已取消箭头，保持简洁布局（如需可再开启） -->
        </g>

        <!-- 固定设施图标：电梯、摄像头、照明、空调（仅绘制，不可编辑） -->
        <g v-for="d in devicesOnFloor" :key="d.id" opacity="0.95" class="devices">
          <!-- 电梯：方框+中线 -->
          <template v-if="d.type==='elevator'">
            <rect :x="d.x - 12" :y="d.y - 12" width="24" height="24" :fill="deviceColor(d.type)" stroke="#222" stroke-width="1" rx="2" ry="2" />
            <line :x1="d.x" :y1="d.y - 10" :x2="d.x" :y2="d.y + 10" stroke="#fff" stroke-width="1.5" />
          </template>
          <!-- 摄像头：机身+镜头 -->
          <template v-else-if="d.type==='camera'">
            <rect :x="d.x - 12" :y="d.y - 8" width="20" height="12" :fill="deviceColor(d.type)" stroke="#222" stroke-width="1" rx="2" ry="2" />
            <circle :cx="d.x + 7" :cy="d.y - 2" r="3" fill="#fff" stroke="#222" stroke-width="1" />
          </template>
          <!-- 照明：灯泡 -->
          <template v-else-if="d.type==='light'">
            <circle :cx="d.x" :cy="d.y - 4" r="8" :fill="deviceColor(d.type)" stroke="#222" stroke-width="1" />
            <rect :x="d.x - 4" :y="d.y + 4" width="8" height="6" fill="#fff" stroke="#222" stroke-width="1" />
          </template>
          <!-- 空调：风扇叶片 -->
          <template v-else-if="d.type==='ac'">
            <circle :cx="d.x" :cy="d.y" r="10" :fill="deviceColor(d.type)" stroke="#222" stroke-width="1" />
            <g stroke="#fff" stroke-width="1.4">
              <line :x1="d.x" :y1="d.y - 6" :x2="d.x" :y2="d.y + 6" />
              <line :x1="d.x - 6" :y1="d.y" :x2="d.x + 6" :y2="d.y" />
              <line :x1="d.x - 4" :y1="d.y - 4" :x2="d.x + 4" :y2="d.y + 4" />
              <line :x1="d.x + 4" :y1="d.y - 4" :x2="d.x - 4" :y2="d.y + 4" />
            </g>
          </template>
        </g>

        </g> <!-- /transformFit group -->
      </svg>

      <div class="legend">
        <span class="badge shop"></span>店铺
        <span class="badge event"></span>活动区
        <span class="badge other"></span>OTHERAREA
        <span class="badge park" style="background:#5cb85c;"></span>车位-空闲
        <span class="badge park" style="background:#d9534f;"></span>车位-占用
        <span class="badge device" style="background:#6c5ce7;"></span>电梯
        <span class="badge device" style="background:#00b894;"></span>摄像头
        <span class="badge device" style="background:#f39c12;"></span>照明
        <span class="badge device" style="background:#0984e3;"></span>空调
      </div>

      <div v-if="hovered" class="tooltip" :style="{ left: hoveredClient.x + 'px', top: hoveredClient.y + 'px' }">
        <div v-if="currentEntity">
          <div><b>ID：</b>{{ currentEntity.id }}</div>
          <div><b>名称：</b>{{ currentEntity.name || currentEntity.label }}</div>
          <div v-if="currentEntity.type"><b>类型：</b>{{ dictLabel(currentEntity.type) }}</div>
          <div v-if="currentEntity.status"><b>状态：</b>{{ currentEntity.status }}</div>
          <div v-if="areaDisplay(currentEntity) != null"><b>面积：</b>{{ areaDisplay(currentEntity) }}</div>
          <div v-if="currentEntity.rent != null"><b>租金：</b>{{ currentEntity.rent }}</div>
        </div>
      </div>
    </div>

    <!-- 右侧属性面板 -->
    <div class="side">
      <h3>区域属性</h3>
      <div v-if="selectedArea">
        <div class="field"><label>ID</label><input v-model="selectedArea.id" /></div>
        <div class="field"><label>名称</label><input v-model="selectedArea.name" /></div>
        <div class="field">
          <label>类型</label>
          <select v-model="selectedArea.type">
            <option value="shop">店铺</option>
            <option value="event">活动区</option>
            <option value="other">OTHERAREA</option>
          </select>
        </div>
        <div class="field"><label>状态</label><input v-model="selectedArea.status" placeholder="空闲/营业/装修…" /></div>
        <div class="field nums">
          <label>X</label><input type="number" v-model.number="metaX" />
          <label>Y</label><input type="number" v-model.number="metaY" />
          <label>W</label><input type="number" v-model.number="metaW" />
          <label>H</label><input type="number" v-model.number="metaH" />
        </div>
        <div class="field nums">
          <label>面积</label><input type="number" min="0" step="1" v-model.number="selectedArea.areaSize" />
          <label>租金</label><input type="number" min="0" step="1" v-model.number="selectedArea.rent" />
        </div>
        <div class="field"><label>备注</label><textarea v-model="selectedArea.remark" rows="3" /></div>
        <div class="ops">
          <button :disabled="!canEdit" @click="persistEdit">保存修改</button>
        </div>
      </div>
      <div v-else class="placeholder">点击画布中的区域进行选择和编辑</div>

      <h3 style="margin-top:24px;">查找</h3>
      <div class="field"><label>关键字</label><input v-model="layerFilter[currentFloor].keyword" placeholder="ID/名称" /></div>
      <ul class="list">
        <li v-for="a in filteredList" :key="a.id" :class="{ active: selectedId===a.id }" @click="select(a.id)">
          <span>{{ a.label }}</span>
          <small>{{ dictLabel(a.type) }} / {{ a.status || '-' }}</small>
        </li>
      </ul>
    </div>

    <!-- 新增区域对话框（简单实现） -->
    <div v-if="showAdd" class="dialog-mask" @click.self="showAdd=false">
      <div class="dialog">
        <h3>新增区域</h3>
        <div class="field"><label>ID</label><input v-model="draft.id" /></div>
        <div class="field"><label>名称</label><input v-model="draft.name" /></div>
        <div class="field">
          <label>类型</label>
          <select v-model="draft.type">
            <option value="shop">店铺</option>
            <option value="event">活动区</option>
            <option value="other">OTHERAREA</option>
          </select>
        </div>
        <div class="field"><label>状态</label><input v-model="draft.status" placeholder="空闲/营业/装修…" /></div>
        <div class="field nums">
          <label>面积</label><input type="number" min="0" step="1" v-model.number="draft.areaSize" />
          <label>租金</label><input type="number" min="0" step="1" v-model.number="draft.rent" />
        </div>
        <div class="field nums">
          <label>X</label><input type="number" v-model.number="draft.x" />
          <label>Y</label><input type="number" v-model.number="draft.y" />
          <label>W</label><input type="number" v-model.number="draft.w" />
          <label>H</label><input type="number" v-model.number="draft.h" />
        </div>
        <div class="ops">
          <button @click="confirmAdd">添加</button>
          <button @click="showAdd=false">取消</button>
        </div>
      </div>
    </div>
  </BoardLayout>
  
</template>

<script setup>
import { reactive, ref, computed, onMounted } from 'vue'
import BoardLayout from '@/components/BoardLayout.vue'
import { MapApi } from '@/services/mapApi'

const canvasSize = reactive({ w: 1200, h: 700 })
// 每层缩放：默认倍率（B1=1.0，1F=0.8，2/3F=1.2），可在滑块上调整并记忆
const layerZoom = reactive({ [-1]: 1.0, [1]: 0.8, [2]: 1.2, [3]: 1.2 })
const currentZoom = computed(() => layerZoom[currentFloor.value])
const currentFloor = ref(1)
// 先不区分权限：全部可编辑
const userRole = ref('admin')
const canEdit = computed(() => true)
// 分层独立筛选缓存（切换记忆）
const layerFilter = reactive({
  [-1]: { type: 'parking', keyword: '' },
  [1]: { type: '', keyword: '' },
  [2]: { type: '', keyword: '' },
  [3]: { type: '', keyword: '' },
})
const selectedId = ref(null)
const hovered = ref(null)
const mouse = reactive({ x: 0, y: 0 })
const dragging = ref(null)
const extraDevices = ref([])

// 默认示例：按提供的示意图构建 B1 与 1F 的大致布局（非精确比例）
function buildDefault1F() {
  const a = []
  // 顶部一排（1-7）使用多边形模拟不规则外形
  let x = 100
  for (let i = 1; i <= 7; i++) {
    a.push({ id: String(i), floor: 1, points: [[x,80],[x+60,80],[x+56,138],[x+6,138]], type: 'shop', status: '空闲' })
    x += 66
  }
  // 顶部大区 8、9、10 采用大块与小块
  // 8 号块做成带弧边的多边形（上边略收、右边圆角感）
  a.push({ id: '8', floor: 1, points: [[640,64],[930,58],[946,78],[948,142],[640,160]], type: 'shop', status: '空闲' })
  // 9 号块右侧向下弧形衔接 10
  a.push({ id: '9', floor: 1, points: [[950,60],[1088,60],[1120,120],[954,158]], type: 'shop', status: '空闲' })
  a.push({ id: '10', floor: 1, points: [[1120,80],[1188,80],[1188,150],[1120,140]], type: 'shop', status: '空闲' })

  // 右侧弧形区（14-27）使用多段多边形沿曲线排列
  let id = 14
  const arcBaseX = 980, arcBaseY = 240
  const rings = [0, 36, 72] // 三条弧带的径向偏移
  rings.forEach((dy, ringIdx) => {
    for (let k = 0; k < 7; k++) {
      const px = arcBaseX + k * 50 + ringIdx * 10
      const py = arcBaseY + dy + Math.sin(k / 6 * Math.PI) * 12
      const poly = [[px,py],[px+42,py-6],[px+46,py+22],[px+4,py+28]]
      a.push({ id: String(id++), floor: 1, points: poly, type: 'shop', status: '空闲' })
    }
  })

  // 中央块（55-66、58-61 等，简化为网格）
  const centerStartX = 520, centerStartY = 220
  const labels = [ ['55','56','57','58'], ['66','65','64','63'] ]
  for (let r = 0; r < 3; r++) {
    for (let c = 0; c < 4; c++) {
      const lab = (labels[r] || [])[c]
      if (!lab) continue
      const x0 = centerStartX + c*70, y0 = centerStartY + r*60
      // 中区做成带倒角的不规则四边形
      a.push({ id: lab, floor: 1, points: [[x0+4,y0],[x0+60,y0+2],[x0+56,y0+42],[x0,y0+44]], type: 'shop', status: '空闲' })
    }
  }
  // 59-61 改为倾斜多边形
  a.push({ id: '59', floor: 1, points: [[800,260],[852,258],[850,304],[802,304]], type: 'shop', status: '空闲' })
  a.push({ id: '60', floor: 1, points: [[860,260],[912,258],[910,304],[862,304]], type: 'shop', status: '空闲' })
  a.push({ id: '61', floor: 1, points: [[920,260],[972,258],[970,304],[922,304]], type: 'shop', status: '空闲' })

  // 底部一排（31-40）
  x = 500
  for (let no = 39; no >= 31; no--) {
    a.push({ id: String(no), floor: 1, points: [[x,420],[x+54,420],[x+54,464],[x,464]], type: 'shop', status: '空闲' })
    x -= 58
  }
  // 左下（44-49）
  x = 260
  for (let no = 44; no <= 49; no++) {
    a.push({ id: String(no), floor: 1, points: [[x,420],[x+54,420],[x+54,464],[x,464]], type: 'shop', status: '空闲' })
    x += 58
  }

  // 左上零散（1F 的 50-54，近入口）
  a.push({ id: '50', floor: 1, points: [[220,240],[264,240],[264,284],[220,284]], type: 'shop', status: '空闲' })
  a.push({ id: '51', floor: 1, points: [[260,240],[304,240],[304,284],[260,284]], type: 'shop', status: '空闲' })
  a.push({ id: '52', floor: 1, points: [[300,240],[344,240],[344,284],[300,284]], type: 'shop', status: '空闲' })
  a.push({ id: '53', floor: 1, points: [[300,200],[344,200],[340,236],[300,236]], type: 'shop', status: '空闲' })
  a.push({ id: '54', floor: 1, points: [[300,160],[344,160],[340,196],[300,196]], type: 'shop', status: '空闲' })

  // 活动区（近 1F 右中区域，以浅黄显示）
  a.push({ id: 'E-1', floor: 1, x: 700, y: 340, w: 180, h: 90, type: 'event', status: '可预订', name: '中庭活动' })
  return a
}

// 将指定楼层的非活动区分散到规则网格，避免重合（不影响 1F）
function distributeFloor(floor, startX, startY, perRow, stepX, stepY) {
  const items = areas.value.filter(a => a.floor === floor && a.type !== 'event')
  items.sort((a,b) => String(a.id).localeCompare(String(b.id)))
  items.forEach((a, idx) => {
    const row = Math.floor(idx / perRow)
    const col = idx % perRow
    const tx = startX + col * stepX
    const ty = startY + row * stepY
    moveShapeTopLeftTo(a, tx, ty)
  })
}
function moveShapeTopLeftTo(a, tx, ty) {
  if (a.points && a.points.length) {
    const b = polyBounds(a)
    const dx = tx - b.x1
    const dy = ty - b.y1
    a.points = a.points.map(([x,y]) => [x + dx, y + dy])
  } else {
    a.x = tx; a.y = ty
  }
}

// 2F / 3F 默认实例（与 1F 类似但不完全相同）
function buildDefault2F() {
  const a = []
  // 顶部不规则块（与 1F 相似但不重复）
  let x = 120
  for (let i = 201; i <= 205; i++) {
    a.push({ id: String(i), floor: 2, points: [[x,90],[x+58,90],[x+54,144],[x+6,144]], type: 'shop', status: i%2? '营业':'空闲' })
    x += 70
  }
  // 顶部大块
  a.push({ id: 'S206', floor: 2, points: [[520,80],[800,80],[802,156],[522,160]], type: 'shop', status: '空闲' })
  a.push({ id: 'S207', floor: 2, points: [[820,86],[980,82],[994,150],[824,150]], type: 'shop', status: '装修' })
  // 中央格块带倒角
  const cX = 480, cY = 220
  for (let r=0;r<2;r++){
    for (let c=0;c<4;c++){
      const x0=cX+c*68, y0=cY+r*58
      a.push({ id: `2C${r}${c}`, floor: 2, points: [[x0+4,y0],[x0+60,y0+2],[x0+56,y0+42],[x0,y0+44]], type: 'shop', status: '空闲' })
    }
  }
  // 活动区
  a.push({ id: 'E-2', floor: 2, points: [[700,330],[880,330],[880,410],[700,410]], type: 'event', status: '布展' })
  // OTHER 区域
  a.push({ id: 'OT2A', floor: 2, points: [[220,240],[300,240],[300,290],[220,290]], type: 'other', status: 'OTHERAREA' })
  return a
}
function buildDefault3F() {
  const a = []
  // 顶部条带与不规则块
  let x = 140
  for (let i = 301; i <= 304; i++) {
    a.push({ id: String(i), floor: 3, points: [[x,100],[x+62,100],[x+60,152],[x,152]], type: 'shop', status: i%2? '营业':'空闲' })
    x += 74
  }
  // 弧形侧带（简化斜四边形序列）
  let id = 320; let sx = 940, sy = 240
  for (let k=0;k<6;k++){
    const px=sx+k*46, py=sy+Math.sin(k/5*Math.PI)*10
    a.push({ id: String(id++), floor: 3, points: [[px,py],[px+40,py-6],[px+42,py+20],[px,py+26]], type: 'shop', status: '空闲' })
  }
  // 中央小活动
  a.push({ id: 'E-3', floor: 3, points: [[540,240],[690,240],[690,320],[540,320]], type: 'event', status: '可预订' })
  // OTHER
  a.push({ id: 'OT3', floor: 3, points: [[180,230],[300,230],[300,280],[180,280]], type: 'other', status: 'OTHERAREA' })
  return a
}

const defaultAreas = [
  ...buildDefault1F(),
  ...buildDefault2F(),
  ...buildDefault3F(),
]

const devices = [
  // 电梯/摄像头/照明/空调，仅绘制
  { id: 'D1', floor: 1, type: 'elevator', x: 360, y: 160 },
  { id: 'D2', floor: 1, type: 'camera',   x: 720, y: 120 },
  { id: 'D6', floor: 1, type: 'light',    x: 620, y: 200 },
  { id: 'D7', floor: 1, type: 'ac',       x: 960, y: 200 },
  // 2F 设备
  { id: 'D2-1', floor: 2, type: 'elevator', x: 340, y: 160 },
  { id: 'D2-2', floor: 2, type: 'camera',   x: 780, y: 140 },
  { id: 'D2-3', floor: 2, type: 'light',    x: 600, y: 210 },
  { id: 'D2-4', floor: 2, type: 'ac',       x: 920, y: 210 },
  // 3F 设备
  { id: 'D3-1', floor: 3, type: 'elevator', x: 320, y: 160 },
  { id: 'D3-2', floor: 3, type: 'camera',   x: 700, y: 140 },
  { id: 'D3-3', floor: 3, type: 'light',    x: 560, y: 220 },
  { id: 'D3-4', floor: 3, type: 'ac',       x: 940, y: 220 },
]

function buildParking(rows = 10, perRow = 30, totalSlots = 144) {
  // 固定网格：10 行 × 30 列（可容纳 300），仅生成前 totalSlots 个
  const arr = []
  const w = 28, h = 16
  const gapX = 8, gapY = 70
  const startX = 0, startY = 60
  const unit = w + gapX
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < perRow; c++) {
      const idx = r * perRow + c + 1
      if (idx > totalSlots) break
      const x = startX + c * unit
      const y = startY + r * gapY
      const skew = -6
      arr.push({ id: `P${idx}`, no: idx, floor: -1, x, y, w, h, occupied: idx % 7 === 0, skew })
    }
  }
  return arr
}

const defaultParking = buildParking(10, 30, 144)

const areas = ref([])
const parkingSlots = ref([])

onMounted(() => {
  const a = localStorage.getItem('mall.editable.areas')
  let p = localStorage.getItem('mall.editable.parking')
  areas.value = a ? JSON.parse(a) : defaultAreas
  parkingSlots.value = p ? JSON.parse(p) : defaultParking
  // 一次性左对齐归一化，避免出现初始大空白
  normalizeLeftEdge(-1)
  normalizeLeftEdge(1)
  normalizeLeftEdge(2)
  normalizeLeftEdge(3)
  ensureFloorCountMatches(2)
  ensureFloorCountMatches(3)
  extraDevices.value = []
  addRegularDevices(2, 4, 3, 260, 120, 180, 90)
  addRegularDevices(3, 4, 3, 240, 120, 180, 90)
  distributeFloor(2, 120, 90, 10, 74, 68)
  distributeFloor(3, 120, 90, 10, 74, 68)
})

const devicesOnFloor = computed(() => {
  const all = [...devices, ...extraDevices.value]
  return all.filter(d => d.floor === currentFloor.value)
})

// 计算内容范围并生成平移transform，让画布左右不再留大白边
const transformFit = computed(() => `translate(0, 0) scale(${currentZoom.value})`)

// 走道/过道线：每层一套参数，含外围虚线框与网格线
const walk = computed(() => {
  const m = 20
  const inner = { x: m, y: m, w: canvasSize.w - 2*m, h: canvasSize.h - 2*m }
  if (currentFloor.value === -1) {
    const p = layoutParams[-1]
    const horizontal = Array.from({ length: p.rows }, (_, r) => p.startY + r * p.stepY - 12)
      .filter(y => y > inner.y && y < inner.y + inner.h)
    return { inner, vertical: [], horizontal }
  }
  if (currentFloor.value === 1) {
    const vertical = [200, 350, 500, 680, 860, 1020]
    const horizontal = [160, 300, 440, 580]
    return { inner, vertical, horizontal }
  }
  const p = layoutParams[currentFloor.value] || { startX: 120, startY: 90, stepX: 74, stepY: 68, rows: 6 }
  const horizontal = Array.from({ length: p.rows }, (_, r) => p.startY + r * p.stepY - 12)
  const vertical = [inner.x + inner.w*0.25, inner.x + inner.w*0.5, inner.x + inner.w*0.75]
  return { inner, vertical, horizontal }
})

const visibleAreas = computed(() => {
  const f = layerFilter[currentFloor.value]
  return areas.value
    .filter(a => a.floor === currentFloor.value)
    .filter(a => !f.type || a.type === f.type)
    .filter(a => !f.keyword || (a.id?.includes(f.keyword) || a.name?.includes(f.keyword)))
    .map(a => ({ ...a, label: `${a.id}${a.name ? '·'+a.name : ''}` }))
})

const filteredList = computed(() => {
  const f = layerFilter[currentFloor.value]
  return areas.value
    .filter(a => a.floor === currentFloor.value)
    .filter(a => !f.type || a.type === f.type)
    .filter(a => !f.keyword || (a.id?.includes(f.keyword) || a.name?.includes(f.keyword)))
    .map(a => ({ ...a, label: `${a.id}${a.name ? '·'+a.name : ''}` }))
})
// 停车场筛选（受 B1 的 layerFilter 影响）
const visibleParking = computed(() => {
  const f = layerFilter[-1]
  return parkingSlots.value.filter(p => {
    const idOk = !f.keyword || String(p.no).includes(f.keyword) || String(p.id).includes(f.keyword)
    if (!f.type || f.type === 'parking') return idOk
    return idOk
  })
})

const selectedArea = computed(() => areas.value.find(a => a.id === selectedId.value))
// 元数据坐标/尺寸：对多边形自动读取其外接矩形，对矩形直接读取
const metaX = computed({
  get(){
    const a = selectedArea.value; if (!a) return 0
    if (a.points && a.points.length) return polyBounds(a).x1
    return Number.isFinite(a.x) ? a.x : 0
  },
  set(v){
    const a = selectedArea.value; if (!a) return
    if (a.points && a.points.length){
      const b = polyBounds(a); const dx = v - b.x1
      a.points = a.points.map(([x,y]) => [x + dx, y])
    } else { a.x = v }
  }
})
const metaY = computed({
  get(){
    const a = selectedArea.value; if (!a) return 0
    if (a.points && a.points.length) return polyBounds(a).y1
    return Number.isFinite(a.y) ? a.y : 0
  },
  set(v){
    const a = selectedArea.value; if (!a) return
    if (a.points && a.points.length){
      const b = polyBounds(a); const dy = v - b.y1
      a.points = a.points.map(([x,y]) => [x, y + dy])
    } else { a.y = v }
  }
})
const metaW = computed({
  get(){
    const a = selectedArea.value; if (!a) return 0
    if (a.points && a.points.length){ const b = polyBounds(a); return b.x2 - b.x1 }
    return Number.isFinite(a.w) ? a.w : 0
  },
  set(v){
    const a = selectedArea.value; if (!a) return
    if (a.points && a.points.length){
      const b = polyBounds(a); const sx = (v <= 0 ? 1 : v / (b.x2 - b.x1 || 1))
      const cx = (b.x1 + b.x2) / 2
      a.points = a.points.map(([x,y]) => [cx + (x - cx) * sx, y])
    } else { a.w = v }
  }
})
const metaH = computed({
  get(){
    const a = selectedArea.value; if (!a) return 0
    if (a.points && a.points.length){ const b = polyBounds(a); return b.y2 - b.y1 }
    return Number.isFinite(a.h) ? a.h : 0
  },
  set(v){
    const a = selectedArea.value; if (!a) return
    if (a.points && a.points.length){
      const b = polyBounds(a); const sy = (v <= 0 ? 1 : v / (b.y2 - b.y1 || 1))
      const cy = (b.y1 + b.y2) / 2
      a.points = a.points.map(([x,y]) => [x, cy + (y - cy) * sy])
    } else { a.h = v }
  }
})
const currentEntity = computed(() => {
  if (!hovered.value) return null
  const a = areas.value.find(x => x.id === hovered.value.id)
  if (a) return a
  const p = parkingSlots.value.find(x => x.id === hovered.value.id)
  return p || null
})

function setFloor(f) { currentFloor.value = f; selectedId.value = null }
function areaFill(a) {
  if (a.type === 'event') return '#ffd37f'
  if (a.type === 'other') return '#bcdffb'
  return a.status === '空闲' ? '#c4f0c5' : (a.status === '装修' ? '#ffe1e1' : '#d8e6ff')
}
function deviceColor(t) {
  return { elevator: '#6c5ce7', camera: '#00b894', light: '#f39c12', ac: '#0984e3' }[t] || '#555'
}
function deviceShort(t) { return { elevator: 'E', camera: 'C', light: 'L', ac: 'A' }[t] || '?' }
function dictLabel(t) { return { shop: '店铺', event: '活动区', parking: '车位', other: 'OTHERAREA' }[t] || t }

// 多边形工具函数
function pointsToString(pts) { return pts.map(p => `${p[0]},${p[1]}`).join(' ') }
function polyBounds(a) {
  const xs = a.points.map(p => p[0]); const ys = a.points.map(p => p[1])
  return { x1: Math.min(...xs), y1: Math.min(...ys), x2: Math.max(...xs), y2: Math.max(...ys) }
}
function labelPos(a) {
  if (a.points && a.points.length) {
    const b = polyBounds(a); return { x: (b.x1 + b.x2) / 2, y: (b.y1 + b.y2) / 2 }
  }
  // 防御：若缺少 x/y/w/h，回退到 1F 中心
  const x = Number.isFinite(a.x) ? a.x : 600
  const y = Number.isFinite(a.y) ? a.y : 300
  const w = Number.isFinite(a.w) ? a.w : 80
  const h = Number.isFinite(a.h) ? a.h : 60
  return { x: x + w / 2, y: y + h / 2 }
}
function polygonArea(pts) {
  let s = 0
  for (let i = 0; i < pts.length; i++) {
    const [x1, y1] = pts[i]; const [x2, y2] = pts[(i + 1) % pts.length]
    s += x1 * y2 - x2 * y1
  }
  return Math.abs(s) / 2
}
function areaDisplay(a) {
  if (a.areaSize != null) return a.areaSize
  if (a.points && a.points.length) return Math.round(polygonArea(a.points))
  if (a.w && a.h) return a.w * a.h
  return null
}

// 统计 1F 分块数量并让目标楼层数量一致（不改变 1F）
function ensureFloorCountMatches(targetFloor) {
  const count1F = areas.value.filter(x => x.floor === 1 && x.type !== 'event').length
  let list = areas.value.filter(x => x.floor === targetFloor && x.type !== 'event')
  if (list.length === count1F) { distributeFloor(targetFloor, layoutParams[targetFloor].startX, layoutParams[targetFloor].startY, layoutParams[targetFloor].perRow, layoutParams[targetFloor].stepX, layoutParams[targetFloor].stepY); return }
  if (list.length < count1F) {
    // 复制本层块以补足数量（轻微偏移避免重叠）
    const need = count1F - list.length
    const base = list.length ? list : areas.value.filter(x => x.floor === 1 && x.type !== 'event').slice(0, 3).map(cloneToFloor(targetFloor))
    for (let i = 0; i < need; i++) {
      const src = base[i % base.length]
      const dup = cloneShape(src)
      jitter(dup, 12 + i * 6, 10)
      areas.value.push(dup)
    }
    list = areas.value.filter(x => x.floor === targetFloor && x.type !== 'event')
  } else {
    // 裁剪多余块（保留前 count1F 个）
    let kept = 0
    areas.value = areas.value.filter(x => {
      if (x.floor !== targetFloor || x.type === 'event') return true
      if (kept < count1F) { kept++; return true }
      return false
    })
    list = areas.value.filter(x => x.floor === targetFloor && x.type !== 'event')
  }
  // 分散定位
  distributeFloor(targetFloor, layoutParams[targetFloor].startX, layoutParams[targetFloor].startY, layoutParams[targetFloor].perRow, layoutParams[targetFloor].stepX, layoutParams[targetFloor].stepY)
}
function cloneToFloor(floor) { return (a) => ({ ...cloneShape(a), floor }) }
function cloneShape(a) {
  if (a.points && a.points.length) return { ...a, points: a.points.map(([x,y]) => [x, y]), id: `${a.id}_c${Math.random().toString(36).slice(2,6)}` }
  return { ...a, id: `${a.id}_c${Math.random().toString(36).slice(2,6)}` }
}
function jitter(a, dx, dy) {
  if (a.points && a.points.length) a.points = a.points.map(([x,y]) => [x + dx, y + dy])
  else { a.x = (a.x || 0) + dx; a.y = (a.y || 0) + dy }
}

// 规律排布设备（rows × cols 网格）
function addRegularDevices(floor, cols, rows, startX, startY, stepX, stepY) {
  const types = ['elevator','camera','light','ac']
  for (let r=0;r<rows;r++){
    for (let c=0;c<cols;c++){
      const t = types[(r*cols+c)%types.length]
      extraDevices.value.push({ id:`DF-${floor}-${r}-${c}`, floor, type: t, x: startX + c*stepX, y: startY + r*stepY })
    }
  }
}

// 将指定楼层的元素整体向左平移，使最小 x 为 0
function normalizeLeftEdge(floor) {
  let minX = Infinity
  if (floor === -1) {
    parkingSlots.value.forEach(p => { minX = Math.min(minX, p.x) })
    if (!Number.isFinite(minX) || minX === 0) return
    parkingSlots.value = parkingSlots.value.map(p => ({ ...p, x: p.x - minX }))
  } else {
    const list = areas.value.filter(a => a.floor === floor)
    list.forEach(a => {
      if (a.points && a.points.length) {
        a.points.forEach(([x]) => { minX = Math.min(minX, x) })
      } else if (Number.isFinite(a.x)) {
        minX = Math.min(minX, a.x)
      }
    })
    if (!Number.isFinite(minX) || minX === 0) return
    areas.value = areas.value.map(a => {
      if (a.floor !== floor) return a
      if (a.points && a.points.length) {
        return { ...a, points: a.points.map(([x,y]) => [x - minX, y]) }
      }
      if (Number.isFinite(a.x)) return { ...a, x: a.x - minX }
      return a
    })
  }
}

function select(id) { selectedId.value = id }
function onAddArea() {
  draft.id = `NEW_${Date.now().toString().slice(-4)}`
  draft.name = ''
  draft.type = 'shop'
  draft.status = '空闲'
  draft.x = 80; draft.y = 260; draft.w = 140; draft.h = 90
  showAdd.value = true
}
function confirmAdd() {
  if (areas.value.some(a => a.id === draft.id)) { alert('ID 已存在，请更换。'); return }
  areas.value.push({ ...draft, floor: currentFloor.value })
  showAdd.value = false
  saveToLocal()
}
function onDuplicate() {
  if (!selectedArea.value) return
  const n = { ...selectedArea.value, id: `${selectedArea.value.id}_copy`, x: selectedArea.value.x + 10, y: selectedArea.value.y + 10 }
  areas.value.push(n)
  select(n.id)
  saveToLocal()
}
function onDelete() {
  areas.value = areas.value.filter(a => a.id !== selectedId.value)
  selectedId.value = null
  saveToLocal()
}
function persistEdit() { saveToLocal() }
function saveToLocal() {
  localStorage.setItem('mall.editable.areas', JSON.stringify(areas.value))
  localStorage.setItem('mall.editable.parking', JSON.stringify(parkingSlots.value))
  // 静默自动保存，不提示
}
function resetToDefault() {
  if (!confirm('将恢复到内置示例布局，确定吗？')) return
  areas.value = JSON.parse(JSON.stringify(defaultAreas))
  parkingSlots.value = JSON.parse(JSON.stringify(defaultParking))
  ensureFloorCountMatches(2)
  ensureFloorCountMatches(3)
  extraDevices.value = []
  addRegularDevices(2, 4, 3, 260, 120, 180, 90)
  addRegularDevices(3, 4, 3, 240, 120, 180, 90)
  distributeFloor(2, 120, 90, 10, 74, 68)
  distributeFloor(3, 120, 90, 10, 74, 68)
  saveToLocal()
}
function togglePark(p) { p.occupied = !p.occupied; saveToLocal() }

function onMouseMove(e) { mouse.x = e.clientX; mouse.y = e.clientY }
const hoveredClient = computed(() => ({ x: mouse.x + 12, y: mouse.y + 12 }))

// 各层分散布局参数（用于 distributeFloor 和过道渲染）
const layoutParams = {
  [-1]: { startX: 0,  startY: 60,  perRow: 30, stepX: 36, stepY: 70, rows: 10 },
  [2]:  { startX: 120, startY: 90,  perRow: 10, stepX: 74, stepY: 68, rows: 6  },
  [3]:  { startX: 120, startY: 90,  perRow: 10, stepX: 74, stepY: 68, rows: 6  },
}

// 简单拖拽移动区域
function startDrag(area, ev) {
  if (currentFloor.value === -1) return
  // 针对多边形，记录起始点，避免未定义 x/y 导致 NaN
  const z = currentZoom.value
  const svg = ev.currentTarget.closest('svg')
  const svgRect = svg.getBoundingClientRect()
  const mouseX = (ev.clientX - svgRect.left) / z
  const mouseY = (ev.clientY - svgRect.top) / z
  const baseX = Number.isFinite(area.x) ? area.x : mouseX
  const baseY = Number.isFinite(area.y) ? area.y : mouseY
  dragging.value = { id: area.id, dx: mouseX - baseX, dy: mouseY - baseY }
  const onMove = (e) => {
    if (!dragging.value) return
    const a = areas.value.find(x => x.id === dragging.value.id)
    if (a) {
      if (a.points && a.points.length) {
        const rect = svg.getBoundingClientRect()
        const curX = (e.clientX - rect.left) / z
        const curY = (e.clientY - rect.top) / z
        const nx = curX - dragging.value.dx
        const ny = curY - dragging.value.dy
        const b = polyBounds(a)
        const cx = (b.x1 + b.x2) / 2
        const cy = (b.y1 + b.y2) / 2
        const dx = nx - cx
        const dy = ny - cy
        a.points = a.points.map(([px, py]) => [px + dx, py + dy])
        // 拖动后保持该楼层分散布局不重叠（对当前层重排）
        distributeFloor(a.floor, layoutParams[a.floor].startX, layoutParams[a.floor].startY, layoutParams[a.floor].perRow, layoutParams[a.floor].stepX, layoutParams[a.floor].stepY)
      } else {
        const rect = svg.getBoundingClientRect()
        const curX = (e.clientX - rect.left) / z
        const curY = (e.clientY - rect.top) / z
        a.x = curX - dragging.value.dx
        a.y = curY - dragging.value.dy
      }
    }
  }
  const onUp = () => { dragging.value = null; window.removeEventListener('mousemove', onMove); window.removeEventListener('mouseup', onUp); saveToLocal() }
  window.addEventListener('mousemove', onMove)
  window.addEventListener('mouseup', onUp)
}

// 新增对话框草稿
const showAdd = ref(false)
const draft = reactive({ id: '', name: '', type: 'shop', status: '空闲', x: 80, y: 260, w: 140, h: 90, areaSize: 0, rent: 0 })

// 导入导出
function exportJSON() {
  const data = { areas: areas.value, parking: parkingSlots.value }
  const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' })
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = 'mall-layout.json'
  a.click()
  URL.revokeObjectURL(url)
}
function importJSON(e) {
  const file = e.target.files && e.target.files[0]
  if (!file) return
  const reader = new FileReader()
  reader.onload = () => {
    try {
      const json = JSON.parse(String(reader.result))
      if (json.areas && json.parking) {
        areas.value = json.areas
        parkingSlots.value = json.parking
        saveToLocal()
      } else { alert('JSON 结构不正确，应包含 areas 与 parking 字段。') }
    } catch (err) { alert('解析 JSON 失败：' + err) }
  }
  reader.readAsText(file)
}

</script>

<style scoped>
.toolbar { display: flex; align-items: center; justify-content: space-between; margin-bottom: 12px; gap: 12px; }
.toolbar .left, .toolbar .right { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
button.active { background: #409eff; color: #fff; }
.canvas-wrap { position: relative; border: 1px solid #e5e5e5; background: #fff; }
.canvas-wrap svg { width: 100%; height: 520px; display: block; }
.devices { pointer-events: none; }
.tooltip { position: fixed; background: rgba(0,0,0,0.75); color: #fff; padding: 6px 8px; border-radius: 4px; font-size: 12px; pointer-events: none; }
.side { margin-top: 12px; border-top: 1px dashed #ddd; padding-top: 12px; }
.field { display: flex; align-items: center; gap: 8px; margin: 8px 0; }
.field label { width: 64px; color: #666; }
.field input, .field select { flex: 1; padding: 6px 8px; border: 1px solid #ccc; border-radius: 4px; }
.field textarea { width: 100%; padding: 6px 8px; border: 1px solid #ccc; border-radius: 4px; }
.field.nums input { width: 80px; }
.ops { display: flex; gap: 8px; }
.placeholder { color: #888; }
.list { list-style: none; padding-left: 0; border: 1px solid #eee; max-height: 220px; overflow:auto; }
.list li { display:flex; justify-content: space-between; padding: 6px 10px; border-bottom: 1px dashed #eee; cursor: pointer; }
.list li.active { background: #f0f6ff; }
.dialog-mask { position: fixed; left:0; top:0; right:0; bottom:0; background: rgba(0,0,0,0.35); display:flex; align-items:center; justify-content:center; }
.dialog { background:#fff; padding:16px; width: 420px; border-radius: 8px; box-shadow: 0 6px 18px rgba(0,0,0,0.2); }
.legend { position:absolute; left:10px; bottom:8px; display:flex; gap:12px; align-items:center; color:#666; }
.badge { width:14px; height:14px; display:inline-block; border:1px solid #ccc; margin-right:4px; border-radius:3px; }
.badge.shop { background:#d8e6ff; }
.badge.event { background:#ffd37f; }
.badge.other { background:#bcdffb; }
.badge.park { background:#5cb85c; }
</style>


