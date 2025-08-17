using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StoreApiTester
{
    public class Program
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string baseUrl = "https://localhost:7071/api"; // 根据你的项目端口调整

        public static async Task Main(string[] args)
        {
            Console.WriteLine("=== Oracle 商户管理系统 API 测试 ===\n");

            try
            {
                // 测试获取可用店面
                await TestGetAvailableAreas();

                // 测试创建新商户
                await TestCreateMerchant();

                // 测试获取所有商户
                await TestGetAllStores();

                // 测试获取单个商户信息
                await TestGetStoreById(1);

                // 测试更新商户状态
                await TestUpdateStoreStatus(1, "歇业中");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"测试过程中发生错误: {ex.Message}");
            }

            Console.WriteLine("\n测试完成，按任意键退出...");
            Console.ReadKey();
        }

        // 测试获取可用店面
        private static async Task TestGetAvailableAreas()
        {
            Console.WriteLine("1. 测试获取可用店面...");
            
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/Store/AvailableAreas");
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"状态码: {response.StatusCode}");
                Console.WriteLine($"响应内容: {content}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取可用店面失败: {ex.Message}\n");
            }
        }

        // 测试创建新商户
        private static async Task TestCreateMerchant()
        {
            Console.WriteLine("2. 测试创建新商户...");
            
            var merchantData = new
            {
                storeName = "测试餐厅",
                storeType = "个人",
                tenantName = "张三",
                contactInfo = "13800138000",
                areaId = 1,
                rentStart = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
                rentEnd = DateTime.Now.AddYears(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                operatorAccount = "admin_account" // 如果有管理员账号的话
            };

            try
            {
                var json = JsonSerializer.Serialize(merchantData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                var response = await httpClient.PostAsync($"{baseUrl}/Store/CreateMerchant", content);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"状态码: {response.StatusCode}");
                Console.WriteLine($"响应内容: {responseContent}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建商户失败: {ex.Message}\n");
            }
        }

        // 测试获取所有商户
        private static async Task TestGetAllStores()
        {
            Console.WriteLine("3. 测试获取所有商户...");
            
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/Store/AllStores");
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"状态码: {response.StatusCode}");
                Console.WriteLine($"响应内容: {content}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取所有商户失败: {ex.Message}\n");
            }
        }

        // 测试获取单个商户信息
        private static async Task TestGetStoreById(int storeId)
        {
            Console.WriteLine($"4. 测试获取商户ID {storeId} 的信息...");
            
            try
            {
                var response = await httpClient.GetAsync($"{baseUrl}/Store/{storeId}");
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"状态码: {response.StatusCode}");
                Console.WriteLine($"响应内容: {content}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取商户信息失败: {ex.Message}\n");
            }
        }

        // 测试更新商户状态
        private static async Task TestUpdateStoreStatus(int storeId, string newStatus)
        {
            Console.WriteLine($"5. 测试更新商户ID {storeId} 的状态为 {newStatus}...");
            
            try
            {
                var response = await httpClient.PatchAsync(
                    $"{baseUrl}/Store/UpdateStoreStatus/{storeId}?newStatus={newStatus}&operatorAccount=admin_account", 
                    null);
                var content = await response.Content.ReadAsStringAsync();
                
                Console.WriteLine($"状态码: {response.StatusCode}");
                Console.WriteLine($"响应内容: {content}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"更新商户状态失败: {ex.Message}\n");
            }
        }
    }
}
