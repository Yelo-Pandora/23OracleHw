// src/stores/user.js
import { defineStore } from 'pinia'

// 从localStorage加载数据并检查是否过期
function loadAndCheckExpiry() {
  const sessionDataJSON = localStorage.getItem('userSession')
  if (!sessionDataJSON) {
    return null // 如果本地没有存储，直接返回null
  }

  try {
    const sessionData = JSON.parse(sessionDataJSON)
    const now = new Date().getTime()

    // 如果当前时间 > 过期时间，则认为已过期
    if (now > sessionData.expiry) {
      localStorage.removeItem('userSession') // 清除已过期的session
      return null
    }

    // 如果未过期，返回存储的真实数据
    return sessionData.data
  } catch (error) {
    // 如果解析失败，也清除掉
    localStorage.removeItem('userSession')
    console.error(error);
    return null
  }
}

export const useUserStore = defineStore('user', {
  state: () => {
    const initialState = loadAndCheckExpiry() // 使用新函数加载初始状态
    return {
      token: initialState?.token || null,
      role: initialState?.role || null,
      userInfo: initialState?.userInfo || null
    }
  },
  actions: {
    // 登录动作
    login(token, role, userInfo) {
      // 更新Pinia state
      this.token = token
      this.role = role
      this.userInfo = userInfo

      // 规定超时的时间
      const EXPIRE_DAYS = 7 // 设置 7 天后过期
      const expiry = new Date().getTime() + (EXPIRE_DAYS * 24 * 60 * 60 * 1000)

      // 将所有数据和过期时间打包成一个对象
      const sessionToStore = {
        data: { token, role, userInfo },
        expiry: expiry
      }

      // 只存入一个键'userSession'
      localStorage.setItem('userSession', JSON.stringify(sessionToStore))
    },
    // 登出动作
    logout() {
      // 更新Pinia state
      this.token = null
      this.role = null
      this.userInfo = null

      // 清除localStorage中的数据
      localStorage.removeItem('userSession')
    }
  }
})

