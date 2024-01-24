using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.ServiceProcess;
using System.Timers;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System.ServiceModel;
using System.Text;
using System.Collections.Generic;
using System.ComponentModel;
using Container = Microsoft.Azure.Cosmos.Container;
using System.Reflection;
using System.IO;
using System.Net;
using Microsoft.Azure.Cosmos.Serialization.HybridRow;
using Microsoft.Azure.Cosmos.Linq;
using System.Net.NetworkInformation;
using System.Linq;
using Refit;
using OP_NS;
using Timer = System.Threading.Timer;
using Microsoft.AspNetCore.Mvc.ModelBinding;

public class IntegracaoController : ControllerBase
{
    public partial class ProtheusAzureIntegrationService
    {
        //List<Product> products;
        bool needMAC = false;
        bool automaticInsert = true;
        bool flagRecondicionamento = false;
        public string user = null;
        string lastMAC = null;
        string lastNS = null;

        StreamWriter localLog;

        private string ADDRESS;
        public static string _0014SPAPISECURITYKEY;
        public static string _0015SPAPISECURITYKEY;
        public static string _0016SPAPISECURITYKEY;

        public class ModelIdResult
        {
            public string ModelString { get; set; }
            public string Id { get; set; }

            public ModelIdResult(string modelString, string id)
            {
                ModelString = modelString;
                Id = id;
            }
        }

        public class HWResult
        {
            public string HardwareVersion { get; set; }
            public string Id { get; set; }

            public HWResult(string id, string hardwareVersion)
            {
                HardwareVersion = hardwareVersion;
                Id = id;
            }
        }
        [Route("api/[controller]")]
        [ApiController]
        [HttpPost]
        public async Task<IActionResult> ReceberDadosProtheus([FromBody] JObject dadosProtheus)
        {
            try
            {
                // Aqui você pode acessar os campos OP, PRODUTO, QUANTIDADE de dadosProtheus
                // e chamar o método "IntegracaoProtheusAzure" com esses dados.

                string op = dadosProtheus["OP"].ToString();
                string produto = dadosProtheus["PRODUTO"].ToString();
                string quantidade = dadosProtheus["QUANTIDADE"].ToString();

                // Chamar o método diretamente, sem criar uma nova instância de IntegracaoController
                await IntegracaoProtheusAzure(op, produto, quantidade);

                //return Ok("Dados recebidos com sucesso!");
            }
            catch (Exception ex)
            {
                //return BadRequest($"Erro ao processar os dados: {ex.Message}");
            }
        }

        public async Task IntegracaoProtheusAzure(string op, string produto, string quantidade)
        {

            try
            {
                //Enabled = false;
                //buttonRegister.Enabled = false;

                string OP = op;
                //string qtd = qtde.Text;
                IntegracaoController integracaoController = new IntegracaoController();
                string codProduto = integracaoController.GetPCBCode(produto);
                string codProdOP = produto;
                //string codProduto = product.Text;
                string modelString = GetModel(produto);
                //string modelString = GetModel(codProduto); //idem de cima
                string hardwareVersion = GetHWVersion(modelString);
                string description = GetDescription(produto);
                string MAC = "000000";
                string MsgBoxSNInfo = null;
                List<AzureProduct> productsAdded = new List<AzureProduct>();
                //string[] serialListArray;
                List<string> serialNumberList;
                int qtdAlreadyRegistered = 0;
                //if (automaticInsert)
                //{
                //    qtdAlreadyRegistered = Convert.ToInt32(textBoxOp.Text.Split('-')[2]);
                //}
                //else
                //{
                qtdAlreadyRegistered = Convert.ToInt32(quantidade);
                //}

                if (!(await OpNonEcziste(OP))) //OP já tem registrado ao menos 1 NS
                {
                    //DialogResult result;
                    AzureAPIService azureAPIServiceMac = new AzureAPIService("AzureProduct");
                    serialNumberList = await azureAPIServiceMac.GetNSByOP(OP);//Pega a quantidade de numero de serie para a OP informada anteriormente.

                    qtdAlreadyRegistered = Convert.ToInt32(quantidade.Split('-')[2].Trim());

                    if (serialNumberList.Count >= qtdAlreadyRegistered)
                    {
                        //CRIAR LOG PARA INFORMAR MENSAGEM ABAIXO
                        //result = MessageBox.Show("Todos os números de série dessa OP já foram registrados anteriormente.\nDeseja continuar?", "Atenção", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        //CRIAR LOG PARA INFORMAR MENSAGEM ABAIXO
                        //result = MessageBox.Show("Parte dos números de série dessa OP já foram registrados anteriormente.\nDeseja continuar?", "Atenção", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    }

                    if (codProduto.Substring(0, 1) == "R")
                    {
                        flagRecondicionamento = true;
                    }
                    else
                    {
                        flagRecondicionamento = false;
                    }

                    int iteration = 1;

                    AzureAPIService azureAPIService = new AzureAPIService("AzureProduct");


                    for (int i = 0; i < Convert.ToInt32(quantidade); i++)
                    {
                        //DESENVOLVER METODO ABAIXO PARA CRIAR NUMERO DE SERIE A PARTIR DO ULTIMO CRIADO
                        //productsAdded.Add(await azureAPIService.GetNewSN(OP));
                    }

                    foreach (AzureProduct product in productsAdded)
                    {
                        var setResult = await SetModel(product.serialNumber, modelString);
                        //CRIAR LOG CONFORME MENSAGEM ABAIXO
                        //await WriteLineProductLog(product.id, "[" + user + "]" + " Número de Série registrado.");
                        if (setResult != null)
                        {
                            var setResultHW = hardwareVersion;

                            if (setResultHW != null)
                            {
                                var productCode = product;
                                var descriptionProduct = description;

                                if (productCode != null && descriptionProduct != null)
                                {
                                    uint macQuantity = NeedMAC(codProdOP);

                                    if (macQuantity > 0)
                                    {

                                        if (await azureAPIService.GetNewMAC(product.id, macQuantity))
                                        {
                                            //CRIAR LOG CONFORME MENSAGEM ABAIXO
                                            //await WriteLineProductLog(product.id, "[" + user + "]" + " MAC Address registrado.");
                                            AzureProduct temporaryAzureProduct = new AzureProduct();
                                            AzureAPIService azureAPIServiceInfo = new AzureAPIService("AzureProduct");
                                            //List<string> productInfo = await azureAPIServiceInfo.GetProductInfo(product.id);

                                            MsgBoxSNInfo += " " + product.serialNumber;
                                            localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                           DateTime.Now.Month.ToString("00") + "/" +
                                                           DateTime.Now.Year.ToString("00") + " " +
                                                           DateTime.Now.Hour.ToString("00") + ":" +
                                                           DateTime.Now.Minute.ToString("00") + ":" +
                                                           DateTime.Now.Second.ToString("00") + "] " +
                                                           "[" + user + "] " + "Registrado Produto NS: " +
                                                           product.serialNumber + " MAC: " + temporaryAzureProduct.MAC1_uint64.ToString("X12"));
                                            iteration++;
                                            lastMAC = (temporaryAzureProduct.MAC1_uint64 + macQuantity - 1).ToString("X12").Substring(6, 6);
                                            lastNS = product.serialNumber;
                                        }
                                        else
                                        {
                                            localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                           DateTime.Now.Month.ToString("00") + "/" +
                                                           DateTime.Now.Year.ToString("00") + " " +
                                                           DateTime.Now.Hour.ToString("00") + ":" +
                                                           DateTime.Now.Minute.ToString("00") + ":" +
                                                           DateTime.Now.Second.ToString("00") + "] " +
                                                           "[" + user + "] " + "Falha ao obter número de MAC Address (NS: " +
                                                           product.serialNumber + ")");
                                        }

                                    }
                                    else
                                    {
                                        MsgBoxSNInfo += " " + product.serialNumber;
                                        localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                           DateTime.Now.Month.ToString("00") + "/" +
                                                           DateTime.Now.Year.ToString("00") + " " +
                                                           DateTime.Now.Hour.ToString("00") + ":" +
                                                           DateTime.Now.Minute.ToString("00") + ":" +
                                                           DateTime.Now.Second.ToString("00") + "] " +
                                                           "[" + user + "] " + "Registrado Produto NS: " +
                                                           product.serialNumber + " MAC: NA");
                                        iteration++;
                                        lastNS = product.serialNumber;
                                    }
                                }
                                else
                                {
                                    localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                           DateTime.Now.Month.ToString("00") + "/" +
                                                           DateTime.Now.Year.ToString("00") + " " +
                                                           DateTime.Now.Hour.ToString("00") + ":" +
                                                           DateTime.Now.Minute.ToString("00") + ":" +
                                                           DateTime.Now.Second.ToString("00") + "] " +
                                                           "[" + user + "] " + "Falha ao registrar código e/ou descrição do produto (NS: " +
                                                           product.serialNumber + ")");
                                }
                            }
                            else
                            {
                                localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                       DateTime.Now.Month.ToString("00") + "/" +
                                                       DateTime.Now.Year.ToString("00") + " " +
                                                       DateTime.Now.Hour.ToString("00") + ":" +
                                                       DateTime.Now.Minute.ToString("00") + ":" +
                                                       DateTime.Now.Second.ToString("00") + "] " +
                                                       "[" + user + "] " + "Falha ao registrar versão de hardware (NS: " +
                                                       product.serialNumber + ")");
                            }
                        }
                        else
                        {
                            localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                               DateTime.Now.Month.ToString("00") + "/" +
                                               DateTime.Now.Year.ToString("00") + " " +
                                               DateTime.Now.Hour.ToString("00") + ":" +
                                               DateTime.Now.Minute.ToString("00") + ":" +
                                               DateTime.Now.Second.ToString("00") + "] " +
                                               "[" + user + "] " + "Falha ao registrar modelo e/ou versão de hardware (NS: " +
                                               product.serialNumber + ")");
                        }
                    }
                }

                else
                {
                    if (codProduto.Substring(0, 1) == "R")
                    {
                        flagRecondicionamento = true;
                    }
                    else
                    {
                        flagRecondicionamento = false;
                    }

                    int iteration = 1;

                    for (int i = 0; i < Convert.ToInt32(quantidade); i++)
                    {
                        try
                        {
                            AzureAPIService azureAPIService = new AzureAPIService("AzureProduct");
                            //productsAdded.Add(await azureAPIService.GetNewSN(OP));
                        }
                        catch (System.Net.Http.HttpRequestException)
                        {
                            //CRIAR LOG
                            //MessageBox.Show("Falha ao conectar com a API");
                            break;
                        }
                    }

                    foreach (AzureProduct product in productsAdded)
                    {
                        var setResult = await SetModel(product.serialNumber, modelString);

                        if (setResult != null)
                        {
                            var setResultHW = hardwareVersion;

                            if (setResultHW != null)
                            {
                                var productCode = product;
                                var descriptionProduct = description;

                                if (productCode != null && descriptionProduct != null)
                                {
                                    uint macQuantity = NeedMAC(codProdOP);

                                    if (macQuantity > 0)
                                    {
                                        AzureAPIService azureAPIServiceMac = new AzureAPIService("AzureProduct");
                                        if (await azureAPIServiceMac.GetNewMAC(product.id, macQuantity))
                                        {
                                            AzureProduct temporaryAzureProduct = new AzureProduct();
                                            AzureAPIService azureAPIServiceInfo = new AzureAPIService("AzureProduct");
                                            //List<string> productInfo = await azureAPIServiceInfo.GetProductInfo(product.id);

                                            MsgBoxSNInfo += " " + product.serialNumber;
                                            localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                           DateTime.Now.Month.ToString("00") + "/" +
                                                           DateTime.Now.Year.ToString("00") + " " +
                                                           DateTime.Now.Hour.ToString("00") + ":" +
                                                           DateTime.Now.Minute.ToString("00") + ":" +
                                                           DateTime.Now.Second.ToString("00") + "] " +
                                                           "[" + user + "] " + "Registrado Produto NS: " +
                                                           product.serialNumber + " MAC: " + temporaryAzureProduct.MAC1_uint64.ToString("X12"));
                                            iteration++;
                                            lastMAC = (temporaryAzureProduct.MAC1_uint64 + macQuantity - 1).ToString("X12").Substring(6, 6);
                                            lastNS = product.serialNumber;
                                        }
                                        else
                                        {
                                            localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                           DateTime.Now.Month.ToString("00") + "/" +
                                                           DateTime.Now.Year.ToString("00") + " " +
                                                           DateTime.Now.Hour.ToString("00") + ":" +
                                                           DateTime.Now.Minute.ToString("00") + ":" +
                                                           DateTime.Now.Second.ToString("00") + "] " +
                                                           "[" + user + "] " + "Falha ao obter número de MAC Address (NS: " +
                                                           product.serialNumber + ")");
                                        }

                                    }
                                    else
                                    {
                                        MsgBoxSNInfo += " " + product.serialNumber;
                                        localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                           DateTime.Now.Month.ToString("00") + "/" +
                                                           DateTime.Now.Year.ToString("00") + " " +
                                                           DateTime.Now.Hour.ToString("00") + ":" +
                                                           DateTime.Now.Minute.ToString("00") + ":" +
                                                           DateTime.Now.Second.ToString("00") + "] " +
                                                           "[" + user + "] " + "Registrado Produto NS: " +
                                                           product.serialNumber + " MAC: NA");
                                        iteration++;
                                        lastNS = product.serialNumber;
                                    }
                                }
                                else
                                {
                                    localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                           DateTime.Now.Month.ToString("00") + "/" +
                                                           DateTime.Now.Year.ToString("00") + " " +
                                                           DateTime.Now.Hour.ToString("00") + ":" +
                                                           DateTime.Now.Minute.ToString("00") + ":" +
                                                           DateTime.Now.Second.ToString("00") + "] " +
                                                           "[" + user + "] " + "Falha ao registrar código e/ou descrição do produto (NS: " +
                                                           product.serialNumber + ")");
                                }
                            }
                            else
                            {
                                localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                                       DateTime.Now.Month.ToString("00") + "/" +
                                                       DateTime.Now.Year.ToString("00") + " " +
                                                       DateTime.Now.Hour.ToString("00") + ":" +
                                                       DateTime.Now.Minute.ToString("00") + ":" +
                                                       DateTime.Now.Second.ToString("00") + "] " +
                                                       "[" + user + "] " + "Falha ao registrar versão de hardware (NS: " +
                                                       product.serialNumber + ")");
                            }
                        }
                        else
                        {
                            localLog.WriteLine("[" + DateTime.Now.Day.ToString("00") + "/" +
                                               DateTime.Now.Month.ToString("00") + "/" +
                                               DateTime.Now.Year.ToString("00") + " " +
                                               DateTime.Now.Hour.ToString("00") + ":" +
                                               DateTime.Now.Minute.ToString("00") + ":" +
                                               DateTime.Now.Second.ToString("00") + "] " +
                                               "[" + user + "] " + "Falha ao registrar modelo e/ou versão de hardware (NS: " +
                                               product.serialNumber + ")");
                        }
                    }
                }
            }

            //Atualizar os campos UltimoNS e UltimoMAC
            //if (Global.PROTHEUS_DB)
            //{
            //    setLastNS(lastNS);
            //    if (NeedMAC(codProdOP) > 0)
            //    {
            //        setLastMAC(lastMAC);
            //    }
            //}

            //COMENTADO POIS NESSE SERVIÇO NÃO IRÁ IMPRIMIR AS ETIQUETAS
            //    DialogResult print = MessageBox.Show("Deseja imprimir as etiquetas dos produtos registrados?", "Atenção!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            //    if (print == DialogResult.Yes)
            //    {
            //        List<AzureProduct> productsToPrint = new List<AzureProduct>();
            //        foreach (AzureProduct product in productsAdded)
            //        {
            //            productsToPrint.Add(await azureAPIServiceforNS.GetProductInfo(product.id));
            //        }

            //        foreach (AzureProduct product in productsToPrint)
            //        {
            //            Print(product.id, product.model, product.productCode, product.MAC1_uint64.ToString("X12"), product.MAC2_uint64.ToString("X12"), product.hardwareVersion);
            //        }
            //        //if (!Global.AZURECOSMOS_DB && Global.PROTHEUS_DB)
            //        //{
            //        //    foreach (Product product in products)
            //        //    {
            //        //        if (!webService.GetProductData(product.Serial))
            //        //        {
            //        //            string model = webService.webServiceData.model;
            //        //            string code = webService.webServiceData.code;
            //        //            string mac = webService.webServiceData.macAddr;
            //        //            string abcc = webService.webServiceData.abcc;
            //        //            string verhw = webService.webServiceData.hardRev;

            //        //            Print(product.Serial, model, code, mac, abcc, verhw);
            //        //        }
            //        //        else
            //        //        {
            //        //            MessageBox.Show("Falha ao se conectar com o servidor");
            //        //        }
            //        //    }
            //        //}

            //    }
            //    status.Text = "OK";
            //    buttonCancel_Click(null, null);
            //}
            catch (Exception exc)
            {
            }
        }

        private uint NeedMAC(string codProdOP)
        {
            switch (codProdOP)
            {
                case "0012594":
                case "0012596":
                case "0035136":
                case "0035146":
                case "0037249":
                case "0053713":
                case "0012189":
                case "0013370":
                case "0035138":
                case "0035148":
                case "0036422":
                case "0060941":
                case "0060946":
                case "0060951":
                case "0060952":
                case "0060981":
                case "0060985":
                case "0060986":
                case "0012190":
                case "0013371":
                case "0035140":
                case "0035150":
                case "0037212":
                case "0053716":
                case "0012191":
                case "0013372":
                case "0035142":
                case "0035152":
                case "0035168":
                case "0036423":
                case "0037398":
                case "0044577":
                case "0053717":
                case "0060947":
                case "0060948":
                case "0060954":
                case "0060955":
                case "0060983":
                case "0060987":
                case "0060988":
                case "0012192":
                case "0035154":
                case "0053736":
                case "0053742":
                case "0012193":
                case "0013375":
                case "0035144":
                case "0035156":
                case "0036424":
                case "0053718":
                case "0060949":
                case "0060950":
                case "0060957":
                case "0060958":
                case "0060984":
                case "0060989":
                case "0060990":
                case "0035158":
                case "0045894":
                case "0036425":
                case "0045895":
                case "0017268":
                case "0018378":
                case "0019958":
                case "0023457":
                case "0032421":
                case "0053848":
                case "0054114":
                case "0017269":
                case "0018445":
                case "0032422":
                case "0053849":
                case "0054115":
                case "0060977":
                case "0060978":
                case "0060993":
                case "0060995":
                case "0017270":
                case "0018446":
                case "0032423":
                case "0053850":
                case "0054116":
                case "0017271":
                case "0018447":
                case "0032424":
                case "0053851":
                case "0054117":
                case "0060979":
                case "0060980":
                case "0060996":
                case "0060997":
                case "0020769":
                case "0032425":
                case "0032430":
                case "0053852":
                case "0054118":
                case "0017273":
                case "0018448":
                case "0031039":
                case "0032426":
                case "0053853":
                case "0054113":
                case "0060975":
                case "0060976":
                case "0060991":
                case "0060992":
                case "0017274":
                case "0018419":
                case "0018449":
                case "0032427":
                case "0035553":
                case "0048846":
                case "0048747":
                case "0042941":
                case "0042967":
                case "0049876":
                case "0049875":
                case "0012078":
                case "0036707":
                case "0038223":
                case "0062462":
                case "0038545":
                case "0038164":
                case "0037743":
                case "0017816":
                case "0055037":
                case "0059360":
                case "R0009260":
                case "R0009261":
                case "R0009262":
                case "R0009263":
                case "R0009264":
                case "R0032421":
                case "R0032422":
                case "R0032423":
                case "R0032424":
                case "R0032425":
                case "R0032426":
                case "R0032427":
                case "R0036422":
                case "R0036423":
                case "R0036424":
                case "R0036425":
                case "R0037212":
                case "R0037249":
                case "R0037398":
                case "0011198":
                case "0020613":
                case "0009260":
                case "0009261":
                case "0009262":
                case "0009263":
                case "0009264":
                case "0009983":
                case "0033459":
                    return 0;

                case "0053654":
                case "0053724":
                case "0053732":
                case "0054025":
                case "0056763":
                case "0053694":
                case "0053714":
                case "0053725":
                case "0053733":
                case "0054026":
                case "0056765":
                case "0060998":
                case "0061001":
                case "0053695":
                case "0053728":
                case "0053734":
                case "0054027":
                case "0056766":
                case "0053696":
                case "0053729":
                case "0053735":
                case "0054028":
                case "0056767":
                case "0060999":
                case "0061002":
                case "0053730":
                case "0053697":
                case "0053731":
                case "0053737":
                case "0054030":
                case "0056768":
                case "0061000":
                case "0061003":
                case "0053804":
                case "0053805":
                case "0053806":
                case "0053807":
                case "0053808":
                case "0053795":
                case "0041055":
                case "0042407":
                case "0041864":
                case "0042408":
                case "0041883":
                case "0042409":
                case "0041935":
                case "0042410":
                case "0041937":
                case "0042411":
                case "0045482":
                case "0059720":
                case "0072429":
                case "0072431":
                case "0069642":
                case "0069637":
                case "0069643":
                case "0069638":
                case "0069644":
                case "0069639":
                case "0069645":
                case "0069640":
                case "0069646":
                case "0069641":
                    return 1;

                case "0072430":
                case "0072432":
                    return 2;

                case "0072017":
                case "0072106":
                    return 4;

                default:
                    throw new Exception("Código de produto não conhecido");
            }
        }

        private async Task<ModelIdResult> SetModel(string serialNumber, string modelString)
        {
            try
            {
                ModelIdResult result = await DefineModel(serialNumber, modelString);
                return result;
            }
            catch
            {
                return new ModelIdResult(null, null); // ou outro valor padrão, dependendo do seu caso
            }
        }

        private Task<ModelIdResult> DefineModel(string serialNumber, string modelString)
        {


            //  Se o NS tiver 6 dígitos, adicionar "00" aos dígitos mais significativos para consulta no banco de dados
            // No ERP só eram salvos 6 dígitos de número de série, enquanto no Azure 8.
            if (serialNumber.Length == 6)
            {
                serialNumber = "00" + serialNumber;
            }
            if (modelString.Contains("+"))
            {
                //model = Uri.EscapeDataString(model);
                //string httpResult = GenericHTTPAzureRequest("/DefineModel/", serialNumber + "@" + model, ADDRESS);

                modelString = modelString.Replace("+", "plus");
                //string httpResult = GenericHTTPAzureRequest("/DefineModel/", id + "@" + modelString, ADDRESS);

                //if (httpResult == "true")
                //{
                //    return true;
                //}
                //else
                //{
                //    return false;
                //}
            }
            else if (modelString.Contains("/"))
            {
                modelString = modelString.Replace("/", ".");
                //string httpResult = GenericHTTPAzureRequest("/DefineModel/", id + "@" + modelString, ADDRESS);

                //if (httpResult == "true")
                //{
                //    return true;
                //}
                //else
                //{
                //    return false;
                //}
            }
            else
            {
                //var APIClient = RestService.For<APIService>(ADDRESS);
                //var header = GetHeaderAssigned(ADDRESS);
                //bool result = await APIClient.DefineModel(serialNumber, model, header);
                //return result;
            }
            return Task.FromResult(new ModelIdResult(modelString, serialNumber));
        }

        private async Task<bool> OpNonEcziste(string oP)
        {
            AzureAPIService azureAPIServiceMac = new AzureAPIService("AzureProduct");
            List<string> products = await azureAPIServiceMac.GetNSByOP(oP);
            if (products != null)
            {
                return false;
            }
            else
            {
                return true;
            }

        }

        private string GetDescription(string produto)
        {
            switch (produto)
            {
                case "0009260":
                    return "PLACA BASE 3101C.CP";
                case "0009261":
                    return "PLACA BASE 3102C.CP";
                case "0009262":
                    return "PLACA BASE 3103C.CP";
                case "0009263":
                    return "PLACA BASE 3104C.CP";
                case "0009264":
                    return "PLACA BASE 3107C.CP";
                case "0009983":
                    return "PLACA BASE 3105C.CP";
                case "0011198":
                    return "INDICADOR 3106C1 TETRACELL";
                case "0012078":
                    return "CAIXA JUNCAO ATE 6 CEL 4136 (INOX)";
                case "0012189":
                    return "INDICADOR DE PESAGEM MOD. 3102CP COM SUP";
                case "0012190":
                    return "INDICADOR DE PESAGEM MOD. 3103CP COM SUP";
                case "0012191":
                    return "INDICADOR DE PESAGEM MOD. 3104CP COM SUP";
                case "0012192":
                    return "INDICADOR DE PESAGEM MOD. 3105CP COM SUP";
                case "0012193":
                    return "INDICADOR DE PESAGEM MOD. 3107CP COM SUP";
                case "0012594":
                    return "INDICADOR DE PESAGEM MOD. 3101CP COM SUP";
                case "0012596":
                    return "INDICADOR 3101CP SEM SUP";
                case "0013370":
                    return "INDICADOR 3102CP SEM SUP";
                case "0013371":
                    return "INDICADOR 3103CP SEM SUP";
                case "0013372":
                    return "INDICADOR 3104CP SEM SUP";
                case "0013375":
                    return "INDICADOR 3107CP SEM SUP";
                case "0017268":
                    return "INDICADOR 3101CS";
                case "0017269":
                    return "INDICADOR 3102CS";
                case "0017270":
                    return "INDICADOR 3103CS";
                case "0017271":
                    return "INDICADOR 3104CS";
                case "0017273":
                    return "INDICADOR 3107CS";
                case "0017274":
                    return "INDICADOR REMOTO 3109CS";
                case "0017816":
                    return "CAIXA JUNCAO MOD. 4154";
                case "0018376":
                    return "PLACA MONTADA 3101C.S";
                case "0018378":
                    return "INDICADOR 3101CS COM RC";
                case "0018419":
                    return "KIT MONT. INTERNA 3109C.S";
                case "0018435":
                    return "PLACA MONTADA 3102C.S";
                case "0018436":
                    return "PLACA MONTADA 3103C.S";
                case "0018437":
                    return "PLACA MONTADA 3104C.S";
                case "0018438":
                    return "PLACA MONTADA 3107C.S";
                case "0018445":
                    return "INDICADOR 3102CS COM RC";
                case "0018446":
                    return "INDICADOR 3103CS COM RC";
                case "0018447":
                    return "INDICADOR 3104CS COM RC";
                case "0018448":
                    return "INDICADOR 3107CS COM RC";
                case "0018449":
                    return "INDICADOR 3109CS COM RC";
                case "0019958":
                    return "INDICADOR PESAGEM MOD.3101C.S FR. CIBI";
                case "0020613":
                    return "CAIXA COM BARREIRAS SEG INTRINSECA 4503";
                case "0020769":
                    return "INDICADOR 3105CS";
                case "0023398":
                    return "PLACA CX.JUNCAO 4136";
                case "0023457":
                    return "INDICADOR 3101C.S CX CP PARA PAINEL SEM AFER";
                case "0031039":
                    return "INDICADOR 3107CS - IP67";
                case "0032038":
                    return "PLACA MONTADA 3105C.S";
                case "0032421":
                    return "PLACA ACABADA 3101C.S";
                case "0032422":
                    return "PLACA ACABADA 3102C.S";
                case "0032423":
                    return "PLACA ACABADA 3103C.S";
                case "0032424":
                    return "PLACA ACABADA 3104C.S";
                case "0032425":
                    return "PLACA ACABADA 3105C.S";
                case "0032426":
                    return "PLACA ACABADA 3107C.S";
                case "0032427":
                    return "PLACA ACABADA 3109C.S";
                case "0032430":
                    return "INDICADOR PES. 3105C.S PARA PAINEL SEM AFER.";
                case "0032858":
                    return "PLACA MONTADA 4154";
                case "0032883":
                    return "PLACA MONTADA MOD 4134A";
                case "0033459":
                    return "DISPLAY INDIC. DE PESO ALFANUMERICO T02 110X28CM";
                case "0035136":
                    return "INDICADOR 3101CP SEM SUP COM RC";
                case "0035138":
                    return "INDICADOR 3102CP SEM SUP COM RC";
                case "0035140":
                    return "INDICADOR 3103CP SEM SUP COM RC";
                case "0035142":
                    return "INDICADOR 3104CP SEM SUP COM RC";
                case "0035144":
                    return "INDICADOR 3107CP SEM SUP COM RC";
                case "0035146":
                    return "INDICADOR 3101CP COM SUP COM RC";
                case "0035148":
                    return "INDICADOR 3102CP COM SUP COM RC";
                case "0035150":
                    return "INDICADOR 3103CP COM SUP COM RC";
                case "0035152":
                    return "INDICADOR 3104CP COM SUP COM RC";
                case "0035154":
                    return "INDICADOR 3105CP COM SUP COM RC";
                case "0035156":
                    return "INDICADOR 3107CP COM SUP COM RC";
                case "0035158":
                    return "INDICADOR 3109CP COM SUP COM RC";
                case "0035168":
                    return "INDICADOR 3105CP PLACA CS";
                case "0035553":
                    return "INDIC DE PESAGEM MOD 3109 CS IP67 SEM SUP";
                case "0035938":
                    return "PLACA MONTADA 3101C.S COM BORNE";
                case "0035939":
                    return "PLACA MONTADA 3102C.S COM BORNE";
                case "0035940":
                    return "PLACA MONTADA 3103C.S COM BORNE";
                case "0035941":
                    return "PLACA MONTADA 3104C.S COM BORNE";
                case "0035942":
                    return "PLACA MONTADA 3105C.S COM BORNE";
                case "0035943":
                    return "PLACA MONTADA 3107C.S COM BORNE";
                case "0035944":
                    return "PLACA MONTADA 3109C.S COM BORNE";
                case "0036422":
                    return "PLACA ACABADA 3102C.S COM BORNE";
                case "0036423":
                    return "PLACA ACABADA 3105C.S COM BORNE";
                case "0036424":
                    return "PLACA ACABADA 3107C.S COM BORNE";
                case "0036425":
                    return "PLACA ACABADA 3109C.S COM BORNE";
                case "0036707":
                    return "MONTAGEM DA CAIXA DE JUNCAO CARRO PALETE";
                case "0037212":
                    return "PLACA ACABADA 3103C.S COM BORNE";
                case "0037249":
                    return "PLACA ACABADA 3101C.S COM BORNE";
                case "0037398":
                    return "PLACA ACABADA 3104C.S COM BORNE";
                case "0037743":
                    return "CAIXA DE JUNCAO TETRACELL MOD.4154A";
                case "0038164":
                    return "CAIXA DE RELES 4424 (4 RELES)";
                case "0038197":
                    return "PLACA MONTADA 4424";
                case "0038223":
                    return "CAIXA DE JUNCAO PARA 4 CELULAS MOD. 4134A";
                case "0038545":
                    return "CAIXA DE RELES PROVA DE EXPLOSAO MOD 4524";
                case "0040655":
                    return "PLACA MONTADA (0002PC) VERSAO UART";
                case "0041055":
                    return "TRANSMISSOR DE PESAGEM 2711-E";
                case "0041818":
                    return "PLACA MONTADA (0002PC) VERSAO RS485 MAIN";
                case "0041864":
                    return "TRANSMISSOR DE PESAGEM 2711-T";
                case "0041883":
                    return "TRANSMISSOR DE PESAGEM 2711-M";
                case "0041935":
                    return "TRANSMISSOR DE PESAGEM 2711-D";
                case "0041937":
                    return "TRANSMISSOR DE PESAGEM 2711-P";
                case "0042407":
                    return "TRANSMISSOR DE PESAGEM 2711-E";
                case "0042408":
                    return "TRANSMISSOR DE PESAGEM 2711-T";
                case "0042409":
                    return "TRANSMISSOR DE PESAGEM 2711-M";
                case "0042410":
                    return "TRANSMISSOR DE PESAGEM 2711-D";
                case "0042411":
                    return "TRANSMISSOR DE PESAGEM 2711-P";
                case "0042941":
                    return "TRANSMISSOR DE PESAGEM MOD. 2710-Mplus";
                case "0042963":
                    return "PLACA MONTADA (0011PC) 2710Mplus";
                case "0042967":
                    return "CONJ. TRANSMISSOR DE PESAGEM 2710-Mplus";
                case "0044577":
                    return "IND 3104CP-B COM RC PARA SINALEIRO";
                case "0044630":
                    return "PLACA MONTADA (0021PC) 2750";
                case "0045482":
                    return "TRANSMISSOR DE PESAGEM AUTOMATICA  2750";
                case "0045894":
                    return "IND REMOTO 3109CP COM SUP";
                case "0045895":
                    return "IND REMOTO 3109CP SEM SUP";
                case "0048653":
                    return "PLACA MONTADA (0031PC) 2710Dplus";
                case "0048774":
                    return "CONJ. TRANSMISSOR DE PESAGEM 2710-Dplus";
                case "0048846":
                    return "TRANSMISSOR DE PESAGEM MOD. 2710-Dplus";
                case "0049686":
                    return "PLACA MONTADA (0035PC) 2710Pplus";
                case "0049875":
                    return "CONJ. TRANSMISSOR DE PESAGEM 2710-Pplus";
                case "0049876":
                    return "TRANSMISSOR DE PESAGEM MOD. 2710-Pplus";
                case "0053654":
                    return "IND 3101CP ETHERNET TCP.IP SEM SUP COM RC";
                case "0053694":
                    return "IND 3102CP ETHERNET TCP.IP SEM SUP COM RC";
                case "0053695":
                    return "IND 3103CP ETHERNET TCP.IP SEM SUP COM RC";
                case "0053696":
                    return "IND 3104CP ETHERNET TCP.IP SEM SUP COM RC";
                case "0053697":
                    return "IND 3107CP ETHERNET TCP.IP SEM SUP COM RC";
                case "0053713":
                    return "PLACA INDICADOR 3101C PARA ETHERNET TCP.IP";
                case "0053714":
                    return "PLACA INDICADOR 3102C PARA ETHERNET TCP.IP";
                case "0053716":
                    return "PLACA INDICADOR 3103C PARA ETHERNET TCP.IP";
                case "0053717":
                    return "PLACA INDICADOR 3104C PARA ETHERNET TCP.IP";
                case "0053718":
                    return "PLACA INDICADOR 3107C PARA ETHERNET TCP.IP";
                case "0053724":
                    return "IND 3101C ETHERNET TCP.IP COM SUP";
                case "0053725":
                    return "IND 3102C ETHERNET TCP.IP COM SUP";
                case "0053728":
                    return "IND 3103C ETHERNET TCP.IP COM SUP";
                case "0053729":
                    return "IND 3104C ETHERNET TCP.IP COM SUP";
                case "0053730":
                    return "IND 3105C ETHERNET TCP.IP COM SUP";
                case "0053731":
                    return "IND 3107C ETHERNET TCP.IP COM SUP";
                case "0053732":
                    return "IND 3101CP ETHERNET TCP.IP COM SUP COM RC";
                case "0053733":
                    return "IND 3102CP ETHERNET TCP.IP COM SUP COM RC";
                case "0053734":
                    return "IND 3103CP ETHERNET TCP.IP COM SUP COM RC";
                case "0053735":
                    return "IND 3104CP ETHERNET TCP.IP COM SUP COM RC";
                case "0053736":
                    return "IND 3105CP ETHERNET TCP.IP COM SUP COM RC";
                case "0053737":
                    return "IND 3107CP ETHERNET TCP.IP COM SUP COM RC";
                case "0053742":
                    return "PLACA INDICADOR 3105C PARA ETHERNET TCP.IP";
                case "0053795":
                    return "IND PESAGEM 3107CS ETHERNET TCP.IP";
                case "0053804":
                    return "IND PESAGEM 3101CS ETHERNET TCP.IP";
                case "0053805":
                    return "IND PESAGEM 3102CS ETHERNET TCP.IP";
                case "0053806":
                    return "IND PESAGEM 3103CS ETHERNET TCP.IP";
                case "0053807":
                    return "IND PESAGEM 3104CS ETHERNET TCP.IP";
                case "0053808":
                    return "IND PESAGEM 3105CS ETHERNET TCP.IP";
                case "0053848":
                    return "PLACA MONTADA 3101CS PARA ETHERNET";
                case "0053849":
                    return "PLACA MONTADA 3102CS PARA ETHERNET";
                case "0053850":
                    return "PLACA MONTADA 3103CS PARA ETHERNET";
                case "0053851":
                    return "PLACA MONTADA 3104CS PARA ETHERNET";
                case "0053852":
                    return "PLACA MONTADA 3105CS PARA ETHERNET";
                case "0053853":
                    return "PLACA MONTADA 3107CS PARA ETHERNET";
                case "0054025":
                    return "INDICADOR 3101CP TCP.IP SEM CABO";
                case "0054026":
                    return "INDICADOR 3102CP TCP.IP SEM CABO";
                case "0054027":
                    return "INDICADOR 3103CP TCP.IP SEM CABO";
                case "0054028":
                    return "INDICADOR 3104CP TCP.IP SEM CABO";
                case "0054030":
                    return "INDICADOR 3107CP TCP.IP SEM CABO";
                case "0054113":
                    return "IND PESAGEM 3107CS ETHERNET TCP.IP COM RC";
                case "0054114":
                    return "IND PESAGEM 3101CS ETHERNET TCP.IP COM RC";
                case "0054115":
                    return "IND PESAGEM 3102CS ETHERNET TCP.IP COM RC";
                case "0054116":
                    return "IND PESAGEM 3103CS ETHERNET TCP.IP COM RC";
                case "0054117":
                    return "IND PESAGEM 3104CS ETHERNET TCP.IP COM RC";
                case "0054118":
                    return "IND PESAGEM 3105CS ETHERNET TCP.IP COM RC";
                case "0055037":
                    return "CAIXA DE JUNCAO PARA 4 CELULAS MOD 4174";
                case "0056763":
                    return "INDICADOR 3101CP TCP.IP SEM CABO COM RC";
                case "0056765":
                    return "INDICADOR 3102CP TCP.IP SEM CABO COM RC";
                case "0056766":
                    return "INDICADOR 3103CP TCP.IP SEM CABO COM RC";
                case "0056767":
                    return "INDICADOR 3104CP TCP.IP SEM CABO COM RC";
                case "0056768":
                    return "INDICADOR 3107CP TCP.IP SEM CABO COM RC";
                case "0059360":
                    return "CAIXA DE JUNCAO PARA 4 CELULAS MOD 4174";
                case "0059720":
                    return "PLACA ACABADA (0021PC) 2750";
                case "0060941":
                    return "INDICADOR 3102CP SEM SUP OPTO";
                case "0060946":
                    return "INDICADOR 3102CP SEM SUP COM RC OPTO";
                case "0060947":
                    return "INDICADOR 3104CP SEM SUP OPTO";
                case "0060948":
                    return "INDICADOR 3104CP SEM SUP COM RC OPTO";
                case "0060949":
                    return "INDICADOR 3107CP SEM SUP OPTO";
                case "0060950":
                    return "INDICADOR 3107CP SEM SUP COM RC OPTO";
                case "0060951":
                    return "INDICADOR 3102CP COM SUP OPTO";
                case "0060952":
                    return "INDICADOR 3102CP COM SUP COM RC OPTO";
                case "0060954":
                    return "INDICADOR 3104CP COM SUP OPTO";
                case "0060955":
                    return "INDICADOR 3104CP COM SUP COM RC OPTO";
                case "0060957":
                    return "INDICADOR 3107CP COM SUP OPTO";
                case "0060958":
                    return "INDICADOR 3107CP COM SUP COM RC OPTO";
                case "0060975":
                    return "INDICADOR 3107CS OPTO";
                case "0060976":
                    return "INDICADOR 3107CS COM RC OPTO";
                case "0060977":
                    return "INDICADOR 3102CS OPTO";
                case "0060978":
                    return "INDICADOR 3102CS COM RC OPTO";
                case "0060979":
                    return "INDICADOR 3104CS OPTO";
                case "0060980":
                    return "INDICADOR 3104CS COM RC OPTO";
                case "0060981":
                    return "IND 3102CP ETHERNET TCP.IP SEM SUP COM RC OPTO";
                case "0060983":
                    return "IND 3104CP ETHERNET TCP.IP SEM SUP COM RC OPTO";
                case "0060984":
                    return "IND 3107CP ETHERNET TCP.IP SEM SUP COM RC OPTO";
                case "0060985":
                    return "IND 3102C ETHERNET TCP.IP COM SUP OPTO";
                case "0060986":
                    return "IND 3102CP ETHERNET TCP.IP COM SUP COM RC OPTO";
                case "0060987":
                    return "IND 3104C ETHERNET TCP.IP COM SUP OPTO";
                case "0060988":
                    return "IND 3104CP ETHERNET TCP.IP COM SUP COM RC OPTO";
                case "0060989":
                    return "IND 3107C ETHERNET TCP.IP COM SUP OPTO";
                case "0060990":
                    return "IND 3107CP ETHERNET TCP.IP COM SUP COM RC OPTO";
                case "0060991":
                    return "IND PESAGEM 3107CS ETHERNET TCP.IP OPTO";
                case "0060992":
                    return "IND PESAGEM 3107CS ETHERNET TCP.IP COM RC OPTO";
                case "0060993":
                    return "IND PESAGEM 3102CS ETHERNET TCP.IP OPTO";
                case "0060995":
                    return "IND PESAGEM 3102CS ETHERNET TCP.IP COM RC OPTO";
                case "0060996":
                    return "IND PESAGEM 3104CS ETHERNET TCP.IP OPTO";
                case "0060997":
                    return "IND PESAGEM 3104CS ETHERNET TCP.IP COM RC OPTO";
                case "0060998":
                    return "INDICADOR 3102CP TCP.IP SEM CABO COM RC OPTO";
                case "0060999":
                    return "INDICADOR 3104CP TCP.IP SEM CABO COM RC OPTO";
                case "0061000":
                    return "INDICADOR 3107CP TCP.IP SEM CABO COM RC OPTO";
                case "0061001":
                    return "INDICADOR 3102CP TCP.IP SEM CABO OPTO";
                case "0061002":
                    return "INDICADOR 3104CP TCP.IP SEM CABO OPTO";
                case "0061003":
                    return "INDICADOR 3107CP TCP.IP SEM CABO OPTO";
                case "0062462":
                    return "PAINEL DE JUNCAO PNEUMATICA";
                case "0072017":
                    return "TRANSMISSOR DE PESAGEM 2712-T";
                case "0072106":
                    return "TRANSMISSOR DE PESAGEM 2714-T";
                case "0072429":
                    return "TRANSMISSOR DE PESAGEM 2712-M";
                case "0072430":
                    return "TRANSMISSOR DE PESAGEM 2712-E";
                case "0072431":
                    return "TRANSMISSOR DE PESAGEM 2714-M";
                case "0072432":
                    return "TRANSMISSOR DE PESAGEM 2714-E";
                case "R0009260":
                    return "PLACA BASE 3101C.CP - RECONDICIONADA";
                case "R0009261":
                    return "PLACA BASE 3102C.CP - RECONDICIONADA";
                case "R0009262":
                    return "PLACA BASE 3103C.CP - RECONDICIONADA";
                case "R0009263":
                    return "PLACA BASE 3104C.CP - RECONDICIONADA";
                case "R0009264":
                    return "PLACA BASE 3107C.CP - RECONDICIONADA";
                case "R0009983":
                    return "PLACA BASE 3105C.CP";
                case "R0018376":
                    return "PLACA MONTADA 3101C.S - EM RECONDICIONAMENTO";
                case "R0018435":
                    return "PLACA MONTADA 3102C.S - EM RECONDICIONAMENTO";
                case "R0018436":
                    return "PLACA MONTADA 3103C.S - EM RECONDICIONAMENTO";
                case "R0018437":
                    return "PLACA MONTADA 3104C.S - EM RECONDICIONAMENTO";
                case "R0018438":
                    return "PLACA MONTADA 3107C.S - EM RECONDICIONAMENTO";
                case "R0018439":
                    return "PLACA MONTADA 3109C.S - EM RECONDICIONAMENTO";
                case "R0032421":
                    return "PLACA ACABADA 3101C.S - RECONDICIONADA";
                case "R0032422":
                    return "PLACA ACABADA 3102C.S - RECONDICIONADA";
                case "R0032423":
                    return "PLACA ACABADA 3103C.S - RECONDICIONADA";
                case "R0032424":
                    return "PLACA ACABADA 3104C.S - RECONDICIONADA";
                case "R0032425":
                    return "PLACA ACABADA 3105C.S - RECONDICIONADA";
                case "R0032426":
                    return "PLACA ACABADA 3107C.S - RECONDICIONADA";
                case "R0032427":
                    return "PLACA ACABADA 3109C.S - RECONDICIONADA";
                case "R0035938":
                    return "PLACA MONTADA 3101C.S COM BORNE - EM RECONDICIONAMENTO";
                case "R0035939":
                    return "PLACA MONTADA 3102C.S COM BORNE - EM RECONDICIONAMENTO";
                case "R0035940":
                    return "PLACA MONTADA 3103C.S COM BORNE - EM RECONDICIONAMENTO";
                case "R0035941":
                    return "PLACA MONTADA 3104C.S COM BORNE - EM RECONDICIONAMENTO";
                case "R0035942":
                    return "PLACA MONTADA 3105C.S COM BORNE - EM RECONDICIONAMENTO";
                case "R0035943":
                    return "PLACA MONTADA 3107C.S COM BORNE - EM RECONDICIONAMENTO";
                case "R0035944":
                    return "PLACA MONTADA 3109C.S COM BORNE - EM RECONDICIONAMENTO";
                case "R0036422":
                    return "PLACA ACABADA 3102C.S COM BORNE - RECONDICIONADA";
                case "R0036423":
                    return "PLACA ACABADA 3105C.S COM BORNE - RECONDICIONADA";
                case "R0036424":
                    return "PLACA ACABADA 3107C.S COM BORNE - RECONDICIONADA";
                case "R0036425":
                    return "PLACA BASE 3109C.CP - EM RECONDICIONAMENTO";
                case "R0037212":
                    return "PLACA ACABADA 3103C.S COM BORNE - RECONDICIONADA";
                case "R0037249":
                    return "PLACA ACABADA 3101C.S COM BORNE - RECONDICIONADA";
                case "R0037398":
                    return "PLACA ACABADA 3104C.S COM BORNE - RECONDICIONADA";
                case "R0051453":
                case "0051453":
                    return "PLACA BASE 3101C.CP - EM RECONDICIONAMENTO";
                case "R0051455":
                case "0051455":
                    return "PLACA BASE 3102C.CP - EM RECONDICIONAMENTO";
                case "R0051457":
                case "0051457":
                    return "PLACA BASE 3103C.CP - EM RECONDICIONAMENTO";
                case "R0051460":
                case "0051460":
                    return "PLACA BASE 3104C.CP - EM RECONDICIONAMENTO";
                case "R0051462":
                case "0051462":
                    return "PLACA BASE 3107C.CP - EM RECONDICIONAMENTO";
                case "0018439":
                    return "PLACA MONTADA 3109C.S";
                case "0072326":
                    return "PLACA MONTADA (0110PC) 2712-FRS";
                case "0072305":
                    return "PLACA MONTADA (0110PC) 2712-FET";
                case "0067102":
                    return "PLACA MONTADA (0118PC) IND 3101D";
                case "0067103":
                    return "PLACA MONTADA (0118PC) IND 3102D";
                case "0067104":
                    return "PLACA MONTADA (0118PC) IND 3103D";
                case "0067105":
                    return "PLACA MONTADA (0118PC) IND 3104D";
                case "0067106":
                    return "PLACA MONTADA (0118PC) IND 3104DS";
                case "0067107":
                    return "PLACA MONTADA (0118PC) IND 3107D";
                case "0067108":
                    return "PLACA MONTADA (0118PC) IND 3107DS";
                default:
                    return null;
            }
        }

        private string GetHWVersion(string modelName)
        {
            switch (modelName)
            {
                case "2711-E":
                case "2711-T":
                case "2711-D":
                case "2711-P":
                case "2711-M":
                    return "11";
                case "2750":
                    return "08";
                case "2710-M+":
                    return "04";
                case "2710-P+":
                    return "02";
                case "2710-D+":
                    return "01";
                case "3101-D":
                case "3102-D":
                case "3103-D":
                case "3104-D":
                case "3104-DS":
                case "3107-D":
                case "3107-DS":
                    return "03";
                default:
                    return "00";
            }
        }

        private string GetModel(string produto)
        {
            string model;

            switch (produto)
            {
                case "0012594":
                case "0012596":
                case "0035136":
                case "0035146":
                case "0037249":
                case "0053713":
                case "R0037249":
                    model = "3101 C";
                    break;
                case "0012189":
                case "0013370":
                case "0035138":
                case "0035148":
                case "0036422":
                case "0060941":
                case "0060946":
                case "0060951":
                case "0060952":
                case "0060981":
                case "0060985":
                case "0060986":
                case "R0036422":
                    model = "3102 C";
                    break;
                case "0012190":
                case "0013371":
                case "0035140":
                case "0035150":
                case "0037212":
                case "0053716":
                case "R0037212":
                    model = "3103 C";
                    break;
                case "0012191":
                case "0013372":
                case "0035142":
                case "0035152":
                case "0035168":
                case "0036423":
                case "0037398":
                case "0044577":
                case "0053717":
                case "0060947":
                case "0060948":
                case "0060954":
                case "0060955":
                case "0060983":
                case "0060987":
                case "0060988":
                case "R0037398":
                    model = "3104 C";
                    break;
                case "0012192":
                case "0035154":
                case "0053736":
                case "0053742":
                case "R0036423":
                    model = "3105 C";
                    break;
                case "0012193":
                case "0013375":
                case "0035144":
                case "0035156":
                case "0036424":
                case "0053718":
                case "0060949":
                case "0060950":
                case "0060957":
                case "0060958":
                case "0060984":
                case "0060989":
                case "0060990":
                case "R0036424":
                    model = "3107 C";
                    break;
                case "0035158":
                case "0045894":
                case "0036425":
                case "0045895":
                case "R0036425":
                    model = "3109 C";
                    break;
                case "0017268":
                case "0018378":
                case "0019958":
                case "0023457":
                case "0032421":
                case "0053848":
                case "0054114":
                case "R0032421":
                    model = "3101 CS";
                    break;
                case "0017269":
                case "0018445":
                case "0032422":
                case "0053849":
                case "0054115":
                case "0060977":
                case "0060978":
                case "0060993":
                case "0060995":
                case "R0032422":
                    model = "3102 CS";
                    break;
                case "0017270":
                case "0018446":
                case "0032423":
                case "0053850":
                case "0054116":
                case "R0032423":
                    model = "3103 CS";
                    break;
                case "0017271":
                case "0018447":
                case "0032424":
                case "0053851":
                case "0054117":
                case "0060979":
                case "0060980":
                case "0060996":
                case "0060997":
                case "R0032424":
                case "R0032425":
                    model = "3104 CS";
                    break;
                case "0020769":
                case "0032425":
                case "0032430":
                case "0053852":
                case "0054118":
                    model = "3105 CS";
                    break;
                case "0017273":
                case "0018448":
                case "0031039":
                case "0032426":
                case "0053853":
                case "0054113":
                case "0060975":
                case "0060976":
                case "0060991":
                case "0060992":
                case "R0032426":
                    model = "3107 CS";
                    break;
                case "0017274":
                case "0018419":
                case "0018449":
                case "0032427":
                case "0035553":
                case "R0032427":
                    model = "3109 CS";
                    break;
                case "0053654":
                case "0053724":
                case "0053732":
                case "0054025":
                case "0056763":
                    model = "3101C TCP/IP";
                    break;
                case "0053694":
                case "0053714":
                case "0053725":
                case "0053733":
                case "0054026":
                case "0056765":
                case "0060998":
                case "0061001":
                    model = "3102C TCP/IP";
                    break;
                case "0053695":
                case "0053728":
                case "0053734":
                case "0054027":
                case "0056766":
                    model = "3103C TCP/IP";
                    break;
                case "0053696":
                case "0053729":
                case "0053735":
                case "0054028":
                case "0056767":
                case "0060999":
                case "0061002":
                    model = "3104C TCP/IP";
                    break;
                case "0053730":
                    model = "3105C TCP/IP";
                    break;
                case "0053697":
                case "0053731":
                case "0053737":
                case "0054030":
                case "0056768":
                case "0061000":
                case "0061003":
                    model = "3107C TCP/IP";
                    break;
                case "0053804":
                    model = "3101CS TCP/IP";
                    break;
                case "0053805":
                    model = "3102CS TCP/IP";
                    break;
                case "0053806":
                    model = "3103CS TCP/IP";
                    break;
                case "0053807":
                    model = "3104CS TCP/IP";
                    break;
                case "0053808":
                    model = "3105CS TCP/IP";
                    break;
                case "0053795":
                    model = "3107CS TCP/IP";
                    break;
                case "0048846":
                case "0048774":
                    model = "2710-D+";
                    break;
                case "0042941":
                case "0042967":
                    model = "2710-M+";
                    break;
                case "0049876":
                case "0049875":
                    model = "2710-P+";
                    break;
                case "0041055":
                case "0042407":
                    model = "2711-E";
                    break;
                case "0041864":
                case "0042408":
                    model = "2711-T";
                    break;
                case "0041883":
                case "0042409":
                    model = "2711-M";
                    break;
                case "0041935":
                case "0042410":
                    model = "2711-D";
                    break;
                case "0041937":
                case "0042411":
                    model = "2711-P";
                    break;
                case "0045482":
                case "0059720":
                    model = "2750";
                    break;
                case "0072429":
                    model = "2712-M";
                    break;
                case "0072430":
                    model = "2712-E";
                    break;
                case "0072017":
                    model = "2712-T";
                    break;
                case "0072431":
                    model = "2714-M";
                    break;
                case "0072432":
                    model = "2714-E";
                    break;
                case "0072106":
                    model = "2714-T";
                    break;
                case "0012078":
                    model = "4136";
                    break;
                case "0036707":
                case "0038223":
                case "0062462":
                    model = "4134-A";
                    break;
                case "0038545":
                    model = "4524";
                    break;
                case "0038164":
                    model = "4424";
                    break;
                case "0037743":
                case "0017816":
                    model = "4154-A";
                    break;
                case "0055037":
                case "0059360":
                    model = "4174";
                    break;
                case "R0009260":
                case "0009260":
                    model = "3101C"; //Placa Base
                    break;
                case "R0009261":
                case "0009261":
                    model = "3102C"; //Placa Base
                    break;
                case "R0009262":
                case "0009262":
                    model = "3103C"; //Placa Base
                    break;
                case "R0009263":
                case "0009263":
                    model = "3104C"; //Placa Base
                    break;
                case "R0009264":
                case "0009264":
                    model = "3107"; //Placa Base
                    break;
                case "0009983":
                    model = "3105C";
                    break;
                case "0011198":
                    model = "3106C1";
                    break;
                case "0020613":
                    model = "4503";
                    break;
                case "0033459":
                    model = "DISPLAY";
                    break;
                case "0069642":
                    model = "3101D";
                    break;
                case "0069637":
                    model = "3101DS";
                    break;
                case "0069643":
                    model = "3102D";
                    break;
                case "0069638":
                    model = "3102DS";
                    break;
                case "0069644":
                    model = "3103D";
                    break;
                case "0069639":
                    model = "3103DS";
                    break;
                case "0069645":
                    model = "3104D";
                    break;
                case "0069640":
                    model = "3104DS";
                    break;
                case "0069646":
                    model = "3107D";
                    break;
                case "0069641":
                    model = "3107DS";
                    break;
                default:
                    throw new Exception("Código de produto não conhecido");
            }

            return model;
        }
    }

    private string GetPCBCode(string produto)
    {
        switch (produto)
        {
            /*--- 3101 C ---*/
            /*-- PLACA ACABADA --*/
            case "0037249":
            case "R0037249":
                return "0037249";
            case "0053713":
                return "0053713";

            /*-- INDICADOR --*/
            case "0012594":
            case "0012596":
            case "0035136":
            case "0035146":
            case "0053654":
            case "0053724":
            case "0053732":
            case "0054025":
            case "0056763":
                return "0035938";

            /*--- 3102 C ---*/
            /*-- PLACA ACABADA --*/
            case "0036422":
            case "R0036422":
                return "0036422";
            case "0053714":
                return "0053714";

            /*-- INDICADOR --*/
            case "0012189":
            case "0013370":
            case "0035138":
            case "0035148":
            case "0053694":
            case "0053725":
            case "0053733":
            case "0054026":
            case "0056765":
            case "0060941":
            case "0060946":
            case "0060951":
            case "0060952":
            case "0060981":
            case "0060985":
            case "0060986":
            case "0060998":
            case "0061001":
                return "0035939";


            /*--- 3103 C ---*/
            /*-- PLACA ACABADA --*/
            case "R0037212":
            case "0037212":
                return "0037212";
            case "0053716":
                return "0053716";

            /*-- INDICADOR --*/
            case "0012190":
            case "0013371":
            case "0035140":
            case "0035150":
            case "0053695":
            case "0053728":
            case "0053734":
            case "0054027":
            case "0056766":
                return "0035940";


            /*--- 3104 C ---*/
            /*-- PLACA ACABADA --*/
            case "0036423":
                return "0036423";
            case "0037398":
            case "R0037398":
                return "0037398";
            case "0053717":
                return "0053717";

            /*-- INDICADOR --*/
            case "0012191":
            case "0053729":
            case "0013372":
            case "0035142":
            case "0035152":
            case "0035168":
            case "0044577":
            case "0053696":
            case "0053735":
            case "0054028":
            case "0056767":
            case "0060947":
            case "0060948":
            case "0060954":
            case "0060955":
            case "0060983":
            case "0060987":
            case "0060988":
            case "0060999":
            case "0061002":
                return "0035941";


            /*--- 3105 C ---*/
            /*-- PLACA ACABADA --*/
            case "0053742":
                return "0053742";
            case "R0036423":
                return "0036423";

            /*-- INDICADOR --*/
            case "0012192":
            case "0035154":
            case "0053730":
            case "0053736":
                return "0035942";


            /*--- 3107 C ---*/
            /*-- PLACA ACABADA --*/
            case "0036424":
            case "R0036424":
                return "0036424";
            case "0053718":
                return "0053718";

            /*-- INDICADOR --*/
            case "0012193":
            case "0013375":
            case "0035144":
            case "0035156":
            case "0053697":
            case "0053731":
            case "0053737":
            case "0054030":
            case "0056768":
            case "0060949":
            case "0060950":
            case "0060957":
            case "0060958":
            case "0060984":
            case "0060989":
            case "0060990":
            case "0061000":
            case "0061003":
                return "0035943";


            /*--- 3109 C ---*/
            /*-- PLACA ACABADA --*/
            case "0036425":
            case "R0036425":
                return "0036425";

            /*-- INDICADOR --*/
            case "0035158":
            case "0045894":
            case "0045895":
                return "0035944";


            /*--- 3101 CS ---*/
            /*-- PLACA ACABADA --*/
            case "0032421":
            case "R0032421":
                return "0032421";
            case "0053848":
                return "0053848";

            /*-- INDICADOR --*/
            case "0017268":
            case "0018378":
            case "0019958":
            case "0023457":
            case "0053804":
            case "0054114":
                return "0018376";


            /*--- 3102 CS ---*/
            /*-- PLACA ACABADA --*/
            case "0032422":
            case "R0032422":
                return "0032422";
            case "0053849":
                return "0053849";

            /*-- INDICADOR --*/
            case "0017269":
            case "0018445":
            case "0053805":
            case "0054115":
            case "0060977":
            case "0060978":
            case "0060993":
            case "0060995":
                return "0018435";


            /*--- 3103 CS ---*/
            /*-- PLACA ACABADA --*/
            case "0032423":
            case "R0032423":
                return "0032423";
            case "0053850":
                return "0053850";

            /*-- INDICADOR --*/
            case "0017270":
            case "0018446":
            case "0053806":
            case "0054116":
                return "0018436";


            /*--- 3104 CS ---*/
            /*-- PLACA ACABADA --*/
            case "0032424":
            case "R0032424":
                return "0032424";
            case "0053851":
                return "0053851";
            case "R0032425":
                return "0032425";

            /*-- INDICADOR --*/
            case "0017271":
            case "0018447":
            case "0053807":
            case "0054117":
            case "0060979":
            case "0060980":
            case "0060996":
            case "0060997":
                return "0018437";


            /*--- 3105 CS ---*/
            /*-- PLACA ACABADA --*/
            case "0032425":
                return "0032425";
            case "0053852":
                return "0053852";

            /*-- INDICADOR --*/
            case "0020769":
            case "0032430":
            case "0053808":
            case "0054118":
                return "0032038";


            /*--- 3107 CS ---*/
            /*-- PLACA ACABADA --*/
            case "0032426":
            case "R0032426":
                return "0032426";
            case "0053853":
                return "0053853";

            /*-- INDICADOR --*/
            case "0017273":
            case "0018448":
            case "0031039":
            case "0053795":
            case "0054113":
            case "0060975":
            case "0060976":
            case "0060991":
            case "0060992":
                return "0018438";


            /*--- 3109 CS ---*/
            /*-- PLACA ACABADA --*/
            case "0032427":
            case "R0032427":
                return "0032427";

            /*-- INDICADOR --*/
            case "0017274":
            case "0018419":
            case "0018449":
            case "0035553":
                return "0018439";


            /*--- 2710-D+ ---*/
            case "0048846":
            case "0048774":
                return "0048653";


            /*--- 2710-M+ ---*/
            case "0042941":
            case "0042967":
                return "0042963";


            /*--- 2710-P+ ---*/
            case "0049876":
            case "0049875":
                return "0049686";


            /*--- 2711-E-P-D-T ---*/
            case "0041055":
            case "0042408":
            case "0042407":
            case "0041864":
            case "0041935":
            case "0042410":
            case "0041937":
            case "0042411":
                return "0040655";


            /*--- 2711-M ---*/
            case "0041883":
            case "0042409":
                return "0041818";


            /*--- 2750 ---*/
            case "0045482":
            case "0059720":
                return "0044630";

            /*--- 2712-M ---*/
            case "0072429":
                return "0072326";

            /*--- 2712-E-T ---*/
            case "0072430":
            case "0072017":
                return "0072305";

            /*--- 2714-M ---*/
            case "0072431":
                return "0072326";

            /*--- 2714-E-T ---*/
            case "0072432":
            case "0072106":
                return "0072305";

            /*--- 4136 ---*/
            case "0012078":
                return "0023398";


            /*--- 4134A e 4174 ---*/
            case "0036707":
            case "0038223":
            case "0059360":
            case "0062462":
            case "0055037":
                return "0032883";


            /*--- 4424 e 4524 ---*/
            case "0038545":
            case "0038164":
                return "0038197";


            /*--- 4154 ---*/
            case "0037743":
            case "0017816":
                return "0032858";


            /*--- Recondicionamento Placa base 3101C ---*/
            case "R0009260":
                return "0051453";


            /*--- Recondicionamento Placa base 3102C ---*/
            case "R0009261":
                return "0051455";


            /*--- Recondicionamento Placa base 3103C ---*/
            case "R0009262":
                return "0051457";


            /*--- Recondicionamento Placa base 3104C ---*/
            case "R0009263":
                return "0051460";


            /*--- Recondicionamento Placa base 3107C ---*/
            case "R0009264":
                return "0051462";


            /*--- Indicador 3106 Tetracell ---*/
            case "0011198":
                return "0011198";


            /*--- Caixa EX 4503 ---*/
            case "0020613":
                return "0020613";


            /*--- Placa base 3101C ---*/
            case "0009260":
                return "0009260";


            /*--- Placa base 3102C ---*/
            case "0009261":
                return "0009261";


            /*--- Placa base 3103C ---*/
            case "0009262":
                return "0009262";


            /*--- Placa base 3104C ---*/
            case "0009263":
                return "0009263";


            /*--- Placa base 3105C ---*/
            case "0009983":
                return "0009983";


            /*--- Placa base 3107C ---*/
            case "0009264":
                return "0009264";


            /*--- Display de área ---*/
            case "0033459":
                return "0033459";

            /*--- INDICADOR 3101D ---*/
            case "0067102":
            case "0069642":
                return "0067102";

            /*--- INDICADOR 3101DS ---*/
            //case "0067102":
            case "0069637":
                return "0067102";

            /*--- INDICADOR 3102D ---*/
            case "0067103":
            case "0069643":
                return "0067103";

            /*--- INDICADOR 3102DS ---*/
            //case "0067103":
            case "0069638":
                return "0067103";

            /*--- INDICADOR 3103D ---*/
            case "0067104":
            case "0069644":
                return "0067104";

            /*--- INDICADOR 3103DS ---*/
            //case "0067104":
            case "0069639":
                return "0067104";

            /*--- INDICADOR 3104D ---*/
            case "0067105":
            case "0069645":
                return "0067105";

            /*--- INDICADOR 3104DS ---*/
            case "0067106":
            case "0069640":
                return "0067106";

            /*--- INDICADOR 3107D ---*/
            case "0067107":
            case "0069646":
                return "0067107";

            /*--- INDICADOR 3107DS ---*/
            case "0067108":
            case "0069641":
                return "0067108";

            default:
                throw new Exception("Código de produto não conhecido");
        }
    }
}