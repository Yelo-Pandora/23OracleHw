// 统一地图接口服务层（预留后端），默认通过 Vite 代理到 /api
// 说明：当前前端仍以内存/localStorage 为主，可将 USE_MOCK 置为 false 直连后端

import axios from 'axios'

const http = axios.create({ baseURL: '/api', timeout: 15000 })
const USE_MOCK = true

function ok(data){ return Promise.resolve({ data }) }

// Schema 约定
// Area: { id, name, type:'shop'|'event'|'other', status, points?: number[][], x?, y?, w?, h?, areaSize?, rent? }
// Device: { id, floor, type:'elevator'|'camera'|'light'|'ac', x, y }
// Parking: { id, no, floor:-1, x, y, w, h, occupied, skew }

export const MapApi = {
  // 楼层列表
  getFloors(){
    if (USE_MOCK) return ok([-1,1,2,3])
    return http.get('/map/floors')
  },

  // 读取指定楼层要素（区域/设备/停车）
  getFloorData(floor){
    if (USE_MOCK){
      const areas = JSON.parse(localStorage.getItem('mall.editable.areas')||'[]').filter(a=>a.floor===floor)
      const parking = JSON.parse(localStorage.getItem('mall.editable.parking')||'[]').filter(p=>p.floor===floor)
      // 设备示例另存一个键
      const devices = JSON.parse(localStorage.getItem('mall.devices')||'[]').filter(d=>d.floor===floor)
      return ok({ areas, devices, parking })
    }
    return http.get(`/map/floors/${floor}`)
  },

  // 批量保存指定楼层区域
  saveAreas(floor, areas){
    if (USE_MOCK){
      const all = JSON.parse(localStorage.getItem('mall.editable.areas')||'[]').filter(a=>a.floor!==floor)
      localStorage.setItem('mall.editable.areas', JSON.stringify([...all, ...areas]))
      return ok({ success:true })
    }
    return http.put(`/map/floors/${floor}/areas`, areas)
  },

  // 单个区域增删改
  upsertArea(floor, area){
    if (USE_MOCK){
      const list = JSON.parse(localStorage.getItem('mall.editable.areas')||'[]').filter(a=>a.floor===floor && a.id!==area.id)
      const others = JSON.parse(localStorage.getItem('mall.editable.areas')||'[]').filter(a=>a.floor!==floor)
      localStorage.setItem('mall.editable.areas', JSON.stringify([...others, ...list, area]))
      return ok(area)
    }
    return http.post(`/map/floors/${floor}/areas`, area)
  },
  deleteArea(floor, areaId){
    if (USE_MOCK){
      const all = JSON.parse(localStorage.getItem('mall.editable.areas')||'[]').filter(a => !(a.floor===floor && a.id===areaId))
      localStorage.setItem('mall.editable.areas', JSON.stringify(all))
      return ok({ success:true })
    }
    return http.delete(`/map/floors/${floor}/areas/${encodeURIComponent(areaId)}`)
  },

  // 设备
  getDevices(floor){
    if (USE_MOCK){
      const devices = JSON.parse(localStorage.getItem('mall.devices')||'[]').filter(d=>d.floor===floor)
      return ok(devices)
    }
    return http.get(`/map/floors/${floor}/devices`)
  },
  saveDevices(floor, devices){
    if (USE_MOCK){
      const others = JSON.parse(localStorage.getItem('mall.devices')||'[]').filter(d=>d.floor!==floor)
      localStorage.setItem('mall.devices', JSON.stringify([...others, ...devices]))
      return ok({ success:true })
    }
    return http.put(`/map/floors/${floor}/devices`, devices)
  },

  // 停车
  getParking(floor){
    if (USE_MOCK){
      const parking = JSON.parse(localStorage.getItem('mall.editable.parking')||'[]').filter(p=>p.floor===floor)
      return ok(parking)
    }
    return http.get(`/map/floors/${floor}/parking`)
  },
  saveParking(floor, parking){
    if (USE_MOCK){
      const others = JSON.parse(localStorage.getItem('mall.editable.parking')||'[]').filter(p=>p.floor!==floor)
      localStorage.setItem('mall.editable.parking', JSON.stringify([...others, ...parking]))
      return ok({ success:true })
    }
    return http.put(`/map/floors/${floor}/parking`, parking)
  },

  // 导入/导出（GeoJSON）
  exportGeoJSON(floor){
    if (USE_MOCK){
      // 由页面拼接，这里仅占位
      return ok({})
    }
    return http.get(`/map/floors/${floor}/export`, { responseType:'blob' })
  },
  importGeoJSON(floor, formData){
    if (USE_MOCK){ return ok({}) }
    return http.post(`/map/floors/${floor}/import`, formData, { headers: { 'Content-Type': 'multipart/form-data' } })
  },
}

export default MapApi


