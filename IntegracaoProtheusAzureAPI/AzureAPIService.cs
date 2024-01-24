using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Refit;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using System.IO;
using System.Net;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Runtime.InteropServices;
using OP_NS;

namespace OP_NS
{

    static class Global
    {
        public const bool PROTHEUS_DB = false;
        public const bool AZURECOSMOS_DB = true;
        public static string _0014SPAPISECURITYKEY;
        public static string _0015SPAPISECURITYKEY;
        public static string _0016SPAPISECURITYKEY;
    }

    #region Azure Product
    public class AzureProduct
    {
        public string id { get; set; }
        public UInt32 serialNumber_uint32 { get; set; }
        public string serialNumber { get; set; }
        public string productionOrder { get; set; }
        public string productCode { get; set; }
        public string model { get; set; }
        public string hardwareVersion { get; set; }
        public string firmwareVersion { get; set; }
        public UInt64 MAC1_uint64 { get; set; }
        public List<string> MAC1 { get; set; }
        public UInt64 MAC2_uint64 { get; set; }
        public List<string> MAC2 { get; set; }
        public UInt64 MAC3_uint64 { get; set; }
        public List<string> MAC3 { get; set; }
        public UInt64 MAC4_uint64 { get; set; }
        public List<string> MAC4 { get; set; }
        public UInt64 MAC5_uint64 { get; set; }
        public List<string> MAC5 { get; set; }
        public string parent { get; set; }
        public List<string> children { get; set; }
        public string description { get; set; }
        public string serialABCC { get; set; }
        public resultTest resultTest { get; set; }
        public CQEFinalRegister CQEFinalRegister { get; set; }
        public VOID VOID { get; set; }
        public string producer { get; set; }

        public string _log { get; set; }

    }

    public class resultTest
    {
        public string testA { get; set; }
        public string testB { get; set; }
        public string testC { get; set; }
    }

    public class CQEFinalRegister
    {
        public string client { get; set; }
        public string NF { get; set; }
        public Instruction Instruction { get; set; }
        public string drawingCode { get; set; }
        public string provider { get; set; }
    }

    public class Instruction
    {
        public string code { get; set; }
        public string revision { get; set; }
    }

    public class VOID
    {
        public string platformModel { get; set; }
        public string INMETRO { get; set; }
        public string date { get; set; }
    }
    #endregion


    public class AzureAPIService
    {
        private string ADDRESS;
        public AzureProduct product = new AzureProduct();
        private Dictionary<string, string> header_0014SP = null;
        private Dictionary<string, string> header_0015SP = null;
        private Dictionary<string, string> header_0016SP = null;

        const string CLIENTSECRET = "8Ae8Q~XpQDY50fdjcJYrMTxL7_QqYaoQmnxU3bfJ";
        const string CLIENTID = "781c17a6-8011-4d86-8aa7-5f24201751ac";
        const string BASESECRETURI = "https://alfainstrumentoskeyvault.vault.azure.net";
        static KeyVaultClient kvc = null;

        //constructor
        public AzureAPIService(string address)
        {
            this.ADDRESS = address;

            //Ação a ser utilizada na Task para obter a subscription key
            Action<object> action = (object obj) =>
            {
                GetSecurityKey(obj.ToString());
            };

            if (address.Contains("0016sp"))
            {
                Task t1 = new Task(action, "0016SP");
                t1.Start();
            }
            else if (address.Contains("0015sp"))
            {
                Task t2 = new Task(action, "0015SP");
                t2.Start();
            }
            else if (address.Contains("0014sp"))
            {
                Task t3 = new Task(action, "0014SP");
                t3.Start();
            }
        }

        public async Task<AzureProduct> GetNewSN(string OP)
        {
            try
            {
                AzureProduct newProduct = await GetSN1(OP);
                return newProduct;
            }
            catch
            {
                return null;
            }
        }

        private async Task<AzureProduct> GetSN1(string OP)
        {
            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            AzureProduct product = await APIClient.GetSN(OP, header);
            return product;
        }

        private void GetSecurityKey(string APIName)
        {
            if (APIName == "0016SP")
            {
                Global._0016SPAPISECURITYKEY = GetAPIMSecurityKey("0016SP");
                header_0016SP = new Dictionary<string, string> { { "Ocp-Apim-Subscription-Key", Global._0016SPAPISECURITYKEY } };
            }
            else if (APIName == "0015SP")
            {
                Global._0015SPAPISECURITYKEY = GetAPIMSecurityKey("0015SP");
                header_0015SP = new Dictionary<string, string> { { "Ocp-Apim-Subscription-Key", Global._0015SPAPISECURITYKEY } };
            }
            else if (APIName == "0014SP")
            {
                Global._0014SPAPISECURITYKEY = GetAPIMSecurityKey("0014SP");
                header_0014SP = new Dictionary<string, string> { { "Ocp-Apim-Subscription-Key", Global._0014SPAPISECURITYKEY } };
            }

        }

        /// <summary>
        /// Obtém a Security Key de cada API no API Manager 
        /// </summary>
        /// <param name="APICode"></param>
        /// <returns></returns>
        public static string GetAPIMSecurityKey(string APICode)
        {
            kvc = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(GetToken));
            SecretBundle secret;
            switch (APICode)
            {
                case "0014SP":
                    secret = Task.Run(() => kvc.GetSecretAsync(BASESECRETURI + @"/secrets/" + "0014SP-APIMSubscriptionKey")).ConfigureAwait(false).GetAwaiter().GetResult();
                    return secret.Value;
                case "0015SP":
                    secret = Task.Run(() => kvc.GetSecretAsync(BASESECRETURI + @"/secrets/" + "0015SP-APIMSubscriptionKey")).ConfigureAwait(false).GetAwaiter().GetResult();
                    return secret.Value;
                case "0016SP":
                    secret = Task.Run(() => kvc.GetSecretAsync(BASESECRETURI + @"/secrets/" + "0016SP-APIMSubscriptionKey")).ConfigureAwait(false).GetAwaiter().GetResult();
                    return secret.Value;

            }

            return null;
        }

        public static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential(CLIENTID, CLIENTSECRET);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }

        public Dictionary<string, string> GetHeaderAssigned(string address)
        {
            if (address.Contains("0016sp")) { return header_0016SP; }
            else if (address.Contains("0015sp")) { return header_0015SP; }
            else if (address.Contains("0014sp")) { return header_0014SP; }
            else { return null; }
        }

        #region API Communication

        private async Task<bool> SetLog(string serialNumber, string log)
        {
            // Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (serialNumber.Length == 6)
            {
                serialNumber = "00" + serialNumber;
            }

            string logEncoded = log.Replace("/", "."); //Azure API App don't accept characters '/', '\' and '%2f'.

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            bool result = await APIClient.SetLog(serialNumber, logEncoded, header);
            return result;
        }

        private async Task<AzureProduct> GetSN(string OP)
        {
            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            AzureProduct product = await APIClient.GetSN(OP, header);
            return product;
        }

        private async Task<List<string>> GetNSbyOP(string OP)
        {
            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            List<string> result = await APIClient.GetNSbyOP(OP, header);
            return result;
        }

        private async Task<AzureProduct> GetNewMAC1(string SN)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (SN.Length == 6)
            {
                SN = "00" + SN;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            AzureProduct product = await APIClient.GetNewMAC1(SN, header);
            return product;
        }

        private async Task<AzureProduct> GetNewMAC2(string SN)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (SN.Length == 6)
            {
                SN = "00" + SN;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            AzureProduct product = await APIClient.GetNewMAC2(SN, header);
            return product;
        }

        private async Task<AzureProduct> GetNewMAC3(string SN)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (SN.Length == 6)
            {
                SN = "00" + SN;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            AzureProduct product = await APIClient.GetNewMAC3(SN, header);
            return product;
        }

        private async Task<AzureProduct> GetNewMAC4(string SN)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (SN.Length == 6)
            {
                SN = "00" + SN;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            AzureProduct product = await APIClient.GetNewMAC4(SN, header);
            return product;
        }

        private async Task<AzureProduct> GetNewMAC5(string SN)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (SN.Length == 6)
            {
                SN = "00" + SN;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            AzureProduct product = await APIClient.GetNewMAC5(SN, header);
            return product;
        }

        private async Task<bool> DefineModel(string serialNumber, string model)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (serialNumber.Length == 6)
            {
                serialNumber = "00" + serialNumber;
            }
            if (model.Contains("+"))
            {
                //model = Uri.EscapeDataString(model);
                //string httpResult = GenericHTTPAzureRequest("/DefineModel/", serialNumber + "@" + model, ADDRESS);

                model = model.Replace("+", "plus");
                string httpResult = GenericHTTPAzureRequest("/DefineModel/", serialNumber + "@" + model, ADDRESS);

                if (httpResult == "true")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (model.Contains("/"))
            {
                model = model.Replace("/", ".");
                string httpResult = GenericHTTPAzureRequest("/DefineModel/", serialNumber + "@" + model, ADDRESS);

                if (httpResult == "true")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                var APIClient = RestService.For<APIService>(ADDRESS);
                var header = GetHeaderAssigned(ADDRESS);
                bool result = await APIClient.DefineModel(serialNumber, model, header);
                return result;
            }
        }

        private async Task<bool> DefineProductCode(string serialNumber, string productCode)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (serialNumber.Length == 6)
            {
                serialNumber = "00" + serialNumber;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            bool result = await APIClient.DefineProductCode(serialNumber, productCode, header);
            return result;
        }

        private async Task<bool> DefineDescription(string serialNumber, string description)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (serialNumber.Length == 6)
            {
                serialNumber = "00" + serialNumber;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            bool result = await APIClient.DefineDescription(serialNumber, description, header);
            return result;
        }

        private async Task<AzureProduct> GetProdInfo(string serialNumber)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (serialNumber.Length == 6)
            {
                serialNumber = "00" + serialNumber;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            AzureProduct product = await APIClient.GetProductInfo(serialNumber, header);
            try
            {
                if (product.model != null)
                {
                    if (product.model.Contains("plus"))
                    {
                        product.model = product.model.Replace("plus", "+");
                    }
                }
            }
            catch
            {
                //ignore product.model == null fail 
            }
            return product;
        }

        private async Task<bool> DefineHWVersion(string serialNumber, string HWver)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (serialNumber.Length == 6)
            {
                serialNumber = "00" + serialNumber;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            bool result = await APIClient.DefineHWVersion(serialNumber, HWver, header);
            return result;
        }

        private async Task<bool> DefineVOIDforBal(string serialNumber, string INMETRO, string platformModel, string date)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (serialNumber.Length == 6)
            {
                serialNumber = "00" + serialNumber;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            bool result = await APIClient.SetVOIDforBal(serialNumber, INMETRO, platformModel, date, header);
            return result;
        }

        private async Task<bool> DefineVOIDforInd(string serialNumber, string INMETRO, string date)
        {
            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (serialNumber.Length == 6)
            {
                serialNumber = "00" + serialNumber;
            }

            var APIClient = RestService.For<APIService>(ADDRESS);
            var header = GetHeaderAssigned(ADDRESS);
            bool result = await APIClient.SetVOIDforInd(serialNumber, INMETRO, date, header);
            return result;
        }

        #endregion


        public async Task<List<string>> GetNSByOP(string OP)
        {
            try
            {
                List<string> productList = await GetNSbyOP(OP);
                return productList;
            }
            catch
            {
                return null;
            }
        }
        public async Task<bool> GetNewMAC(string serialNumber, uint quantity)
        {
            try
            {
                bool ret = false;

                switch (quantity)
                {
                    case 5:
                        {
                            AzureProduct product = await GetNewMAC5(serialNumber);
                            if (product.MAC5_uint64 > 123502841561088)
                            {
                                ret = true;
                                goto case 4; // fall through
                            }
                            else
                            {
                                ret = false;
                                break;
                            }
                        }

                    case 4:
                        {
                            AzureProduct product = await GetNewMAC4(serialNumber);
                            if (product.MAC4_uint64 > 123502841561088)
                            {
                                ret = true;
                                goto case 3; // fall through
                            }
                            else
                            {
                                ret = false;
                                break;
                            }
                        }

                    case 3:
                        {
                            AzureProduct product = await GetNewMAC3(serialNumber);
                            if (product.MAC3_uint64 > 123502841561088)
                            {
                                ret = true;
                                goto case 2; // fall through
                            }
                            else
                            {
                                ret = false;
                                break;
                            }
                        }

                    case 2:
                        {
                            AzureProduct product = await GetNewMAC2(serialNumber);
                            if (product.MAC2_uint64 > 123502841561088)
                            {
                                ret = true;
                                goto case 1; // fall through
                            }
                            else
                            {
                                ret = false;
                                break;
                            }
                        }

                    case 1:
                        {
                            AzureProduct product = await GetNewMAC1(serialNumber);
                            if (product.MAC1_uint64 > 123502841561088)
                            {
                                ret = true;
                            }
                            else
                            {
                                ret = false;
                            }
                        }
                        break;

                    default:
                        break;
                }

                return ret;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SetModel(string serialNumber, string model)
        {
            try
            {
                bool result = await DefineModel(serialNumber, model);
                return result;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SetProductCode(string serialNumber, string productCode)
        {
            try
            {
                bool result = await DefineProductCode(serialNumber, productCode);
                return result;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SetDescription(string serialNumber, string description)
        {
            try
            {
                bool result = await DefineDescription(serialNumber, description);
                return result;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> WriteLineProductLog(string SerialNumber, string Text)
        {
            // Adiciona um registro de evento no servidor.
            // Cada evento possui informações de data e hora do ocorrido.

            bool error = true;
            string log;

            // Utiliza o carácter '#' como quebra de linha. Necessário pois os caracteres '\r' e '\n' não são interpretados pelo servidor.
            log = "[" + DateTime.Now.Day.ToString("00") + "/" +
                         DateTime.Now.Month.ToString("00") + "/" +
                         DateTime.Now.Year.ToString("00") + " " +
                         DateTime.Now.Hour.ToString("00") + ":" +
                         DateTime.Now.Minute.ToString("00") + ":" +
                         DateTime.Now.Second.ToString("00") + "]" +
                         Text;
            bool result = await SetLog(SerialNumber, log);
            if (result == false)
            {
                error = false;
            }

            return error;
        }

        public async Task<AzureProduct> GetProductInfo(string productID)
        {
            try
            {
                return await GetProdInfo(productID);
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> SetHWVer(string serialNumber, string HWver)
        {
            try
            {
                bool result = await DefineHWVersion(serialNumber, HWver);
                return result;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SetVOIDforBal(string serialNumber, string INMETRO, string platformModel, string date)
        {
            try
            {
                bool result = await DefineVOIDforBal(serialNumber, INMETRO, platformModel, date);
                return result;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> SetVOIDforInd(string serialNumber, string INMETRO, string date)
        {
            try
            {
                bool result = await DefineVOIDforInd(serialNumber, INMETRO, date);
                return result;
            }
            catch
            {
                return false;
            }
        }

        private string GenericHTTPAzureRequest(string command, string payload, string address)
        {
            try
            {
                Uri url = new Uri(address + command + payload);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.KeepAlive = false;

                req.Headers.Add("Ocp-Apim-Subscription-Key: " + Global._0014SPAPISECURITYKEY);
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();

                Stream dataStream = res.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadLine();

                reader.Close();
                reader.Dispose();
                req.Abort();
                res.Close();
                res.Dispose();

                return responseFromServer;
            }
            catch
            {
                return null;
            }
        }

    }
    public interface APIService
    {

        [Get("/SetLog/{serialNumber}&{log}")]
        Task<bool> SetLog(string serialNumber, string log, [HeaderCollection] IDictionary<string, string> header);

        [Get("/newProduct@{productionOrder}")]
        Task<AzureProduct> GetSN(string productionOrder, [HeaderCollection] IDictionary<string, string> header);

        [Get("/GetNSbyOP/{id}")]
        Task<List<string>> GetNSbyOP(string id, [HeaderCollection] IDictionary<string, string> header);

        [Get("/setMAC1?id={serialNumber}")]
        Task<AzureProduct> GetNewMAC1(string serialNumber, [HeaderCollection] IDictionary<string, string> header);

        [Get("/setMAC2?id={serialNumber}")]
        Task<AzureProduct> GetNewMAC2(string serialNumber, [HeaderCollection] IDictionary<string, string> header);

        [Get("/setMAC3?id={serialNumber}")]
        Task<AzureProduct> GetNewMAC3(string serialNumber, [HeaderCollection] IDictionary<string, string> header);

        [Get("/setMAC4?id={serialNumber}")]
        Task<AzureProduct> GetNewMAC4(string serialNumber, [HeaderCollection] IDictionary<string, string> header);

        [Get("/setMAC5?id={serialNumber}")]
        Task<AzureProduct> GetNewMAC5(string serialNumber, [HeaderCollection] IDictionary<string, string> header);

        [Get("/DefineModel/{id}@{model}")]
        Task<bool> DefineModel(string id, string model, [HeaderCollection] IDictionary<string, string> header);

        [Get("/DefineProductCode/{id}@{code}")]
        Task<bool> DefineProductCode(string id, string code, [HeaderCollection] IDictionary<string, string> header);

        [Get("/DefineHWVersion/{id}@{code}")]
        Task<bool> DefineHWVersion(string id, string code, [HeaderCollection] IDictionary<string, string> header);

        [Get("/DefineDescription/{id}@{description}")]
        Task<bool> DefineDescription(string id, string description, [HeaderCollection] IDictionary<string, string> header);

        [Get("/products/{id}")]
        Task<AzureProduct> GetProductInfo(string id, [HeaderCollection] IDictionary<string, string> header);

        [Get("/SetVOIDforBal/{id}@{INMETRO}&{platformModel}&{date}")]
        Task<bool> SetVOIDforBal(string id, string INMETRO, string platformModel, string date, [HeaderCollection] IDictionary<string, string> header);

        [Get("/SetVOIDforInd/{id}@{INMETRO}&{date}")]
        Task<bool> SetVOIDforInd(string id, string INMETRO, string date, [HeaderCollection] IDictionary<string, string> header);

    }

}
