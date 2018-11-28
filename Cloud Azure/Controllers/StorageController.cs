using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using MimeDetective;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Cloud_Azure.Controllers
{
    [System.Web.Http.RoutePrefix("api")]
    public class StorageController : ApiController
    {
        #region Auxiliary Properties

        private static string ResourceIdAzure
        {
            get
            {
                var urlResourceId = ConfigurationManager.AppSettings["ResourceId"];
                if (string.IsNullOrEmpty(urlResourceId))
                {
                    throw new InvalidOperationException("URL do Azure (ResourceId) não foi definida no arquivo web.config.");
                }

                return urlResourceId;
            }
        }

        private static string AuthEndpointAzure
        {
            get
            {
                var urlAuthEndpoint = ConfigurationManager.AppSettings["AuthEndpoint"];
                if (string.IsNullOrEmpty(urlAuthEndpoint))
                {
                    throw new InvalidOperationException("URL do Azure (AuthEndpoint) não foi definida no arquivo web.config.");
                }

                return urlAuthEndpoint;
            }
        }

        private static string TenantIdAzure
        {
            get
            {
                var tenantId = ConfigurationManager.AppSettings["TenantId"];
                if (string.IsNullOrEmpty(tenantId))
                {
                    throw new InvalidOperationException("Configuração do Azure (TenantId) não foi definida no arquivo web.config.");
                }

                return tenantId;
            }
        }

        private static string ClientIdAzure
        {
            get
            {
                var clientId = ConfigurationManager.AppSettings["ClientId"];
                if (string.IsNullOrEmpty(clientId))
                {
                    throw new InvalidOperationException("Configuração do Azure (ClientId) não foi definida no arquivo web.config.");
                }

                return clientId;
            }
        }

        private static string ClientSecretAzure
        {
            get
            {
                var clientSecret = ConfigurationManager.AppSettings["ClientSecret"];
                if (string.IsNullOrEmpty(clientSecret))
                {
                    throw new InvalidOperationException("Configuração do Azure (ClientSecret) não foi definida no arquivo web.config.");
                }

                return clientSecret;
            }
        }

        private static string ContainerNameAzure
        {
            get
            {
                var containerName = ConfigurationManager.AppSettings["ContainerName"];
                if (string.IsNullOrEmpty(containerName))
                {
                    throw new InvalidOperationException("Configuração do Azure (ContainerName) não foi definida no arquivo web.config.");
                }

                return containerName;
            }
        }

        private static string BlobStorageAzure
        {
            get
            {
                var blobStorage = ConfigurationManager.AppSettings["BlobStorage"];
                if (string.IsNullOrEmpty(blobStorage))
                {
                    throw new InvalidOperationException("Configuração do Azure (BlobStorage) não foi definida no arquivo web.config.");
                }

                return blobStorage;
            }
        }
        private static string SourceFolder
        {
            get
            {
                var sourceFolder = ConfigurationManager.AppSettings["sourceFolder"];
                if (string.IsNullOrEmpty(sourceFolder))
                {
                    throw new InvalidOperationException("Configuração do diretório para salvar arquivos não foi definida no arquivo web.config.");
                }

                return sourceFolder;
            }
        }
        private static string AccountNameAzure
        {
            get
            {
                var accountName = ConfigurationManager.AppSettings["AccountName"];
                if (string.IsNullOrEmpty(accountName))
                {
                    throw new InvalidOperationException("Configuração do Azure (AccountName) não foi definida no arquivo web.config.");
                }

                return accountName;
            }
        }
        private static string AccountKeyAzure
        {
            get
            {
                var accountKey = ConfigurationManager.AppSettings["AccountKey"];
                if (string.IsNullOrEmpty(accountKey))
                {
                    throw new InvalidOperationException("Configuração do Azure (BlobStorage) não foi definida no arquivo web.config.");
                }

                return accountKey;
            }
        }
        #endregion

        /// <summary>
        /// Azure Token Returns
        /// </summary>
        ///  <remarks>Method that returns the Azure authentication Token</remarks>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("GET")]
        [System.Web.Http.Route("GenerateToken")]
        public async Task<HttpResponseMessage> AutenticateAzure()
        {
            HttpResponseMessage response = new HttpResponseMessage();
            string jsonresponse = string.Empty;
            string token = string.Empty;

            try
            {
                token = GetCredential();

                jsonresponse = JsonConvert.SerializeObject(new { token });
                response.Content = new StringContent(jsonresponse, Encoding.UTF8, "application/json");
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (InvalidOperationException e)
            {
                response.StatusCode = HttpStatusCode.PreconditionFailed;
                response.Content = new StringContent(
                    JsonConvert.SerializeObject($"Erro ao obter token: {e.Message}"),
                    Encoding.UTF8, "application/json");
            }
            catch (Exception e)
            {
                response.StatusCode = HttpStatusCode.PreconditionFailed;
                response.Content = new StringContent(
                    JsonConvert.SerializeObject($"Erro Desconhecido: {e.Message}"),
                    Encoding.UTF8, "application/json");
            }

            return response;
        }

        /// <summary>
        /// Upload file with app key and app
        /// </summary>
        /// <returns></returns>
        [System.Web.Http.AcceptVerbs("Post")]
        [System.Web.Http.Route("UploadWithAppKey")]
        public async Task<HttpResponseMessage> UploadWithAppKey()
        {
            HttpResponseMessage response = new HttpResponseMessage();

            var storageAccount = new CloudStorageAccount(new StorageCredentials(AccountNameAzure, AccountKeyAzure), true);

            //Create azure file share
            var share = storageAccount.CreateCloudFileClient().GetShareReference("arquivos"); //file services
            share.CreateIfNotExists();

            //Create file in root directory
            var rootDir = share.GetRootDirectoryReference();
            rootDir.GetFileReference("rootfile.txt").UploadText("The root file content");

            //Get folders with files
            var folder1 = rootDir.GetDirectoryReference("2017");
            await folder1.CreateIfNotExistsAsync();

            folder1.GetFileReference("file1.txt").UploadText("File1 content");

            //Download all itens
            DownloadFile(rootDir, SourceFolder);

            response.Content = new StringContent("Upload Realizado", Encoding.UTF8, "application/json");
            response.StatusCode = HttpStatusCode.OK;

            return response;

        }
        private static void DownloadFile(CloudFileDirectory rooDir, string path)
        {
            foreach (var fileItem in rooDir.ListFilesAndDirectories())
            {
                if (fileItem is CloudFile file)
                {
                    file.DownloadToFile(Path.Combine(path, file.Name), FileMode.Create);
                }
                else if (fileItem is CloudFileDirectory dir)
                {
                    var dirPath = Path.Combine(path, dir.Name);
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    DownloadFile(dir, dirPath);
                }
            }
        }

        // GET: Storage
        [System.Web.Http.AcceptVerbs("Post")]
        [System.Web.Http.Route("UploadWithToken")]
        public async Task<HttpResponseMessage> UploadWithToken()
        {
            JObject objJson = new JObject();
            List<string> lstPathFile = new List<string>();
            byte[] filebytes = null;
            HttpResponseMessage response = new HttpResponseMessage();
            string jsonresponse = string.Empty;

            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }
            if ((Request.Content.Headers.ContentLength / 1024) > 30720)
            {
                objJson["msg"] = "Tamanho do(s) arquivo(s) para upload excedeu a capacidade permitida";
                throw new InvalidOperationException(objJson["msg"].ToString());
            }

            try
            {
                var provider = new MultipartMemoryStreamProvider();
                await Request.Content.ReadAsMultipartAsync(provider);

                foreach (var file in provider.Contents)
                {
                    var buffer = await file.ReadAsByteArrayAsync();

                    FileType fileType = buffer.GetFileType();

                    //upload file in Azure
                    var filePath = await UploadFileAzure(filebytes, fileType.Extension);
                    lstPathFile.Add(filePath);
                }

                jsonresponse = JsonConvert.SerializeObject(new { lstPathFile });
                response.Content = new StringContent(jsonresponse, Encoding.UTF8, "application/json");
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (InvalidOperationException e)
            {
                response.StatusCode = HttpStatusCode.PreconditionFailed;
                response.Content = new StringContent(
                    JsonConvert.SerializeObject($"Erro ao obter token: {e.Message}"),
                    Encoding.UTF8, "application/json");
            }
            catch (Exception e)
            {
                response.StatusCode = HttpStatusCode.PreconditionFailed;
                response.Content = new StringContent(
                    JsonConvert.SerializeObject($"Erro Desconhecido: {e.Message}"),
                    Encoding.UTF8, "application/json");
            }

            return response;
        }

        [System.Web.Http.AcceptVerbs("Post")]
        [System.Web.Http.Route("DownloadSpecificFileWithToken")]
        // GET: Storage
        public async Task<HttpResponseMessage> DownloadSpecificFileWithToken(string filePathAzure)
        {
            JObject objJson = new JObject();
            List<string> lstPathFile = new List<string>();
            byte[] filebytes = null;
            HttpResponseMessage response = new HttpResponseMessage();
            string jsonresponse = string.Empty;

            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }
            if ((Request.Content.Headers.ContentLength / 1024) > 30720)
            {
                objJson["msg"] = "Tamanho do(s) arquivo(s) para upload excedeu a capacidade permitida";
                throw new InvalidOperationException(objJson["msg"].ToString());
            }

            try
            {
                var filePath = DownloadFileAzure(filePathAzure);

                jsonresponse = JsonConvert.SerializeObject(new { lstPathFile });
                response.Content = new StringContent(jsonresponse, Encoding.UTF8, "application/json");
                response.StatusCode = HttpStatusCode.OK;
            }
            catch (InvalidOperationException e)
            {
                response.StatusCode = HttpStatusCode.PreconditionFailed;
                response.Content = new StringContent(
                    JsonConvert.SerializeObject($"Erro ao obter token: {e.Message}"),
                    Encoding.UTF8, "application/json");
            }
            catch (Exception e)
            {
                response.StatusCode = HttpStatusCode.PreconditionFailed;
                response.Content = new StringContent(
                    JsonConvert.SerializeObject($"Erro Desconhecido: {e.Message}"),
                    Encoding.UTF8, "application/json");
            }

            return response;
        }

        private static string GetCredential()
        {
            // Construct the authority string from the Azure AD OAuth endpoint and the tenant ID. 
            string authority = string.Format(CultureInfo.InvariantCulture, AuthEndpointAzure, TenantIdAzure);
            AuthenticationContext authContext = new AuthenticationContext(authority);

            ClientCredential clientCredential = new ClientCredential(ClientIdAzure, ClientSecretAzure);

            // Acquire an access token from Azure AD. 
            AuthenticationResult result = authContext.AcquireTokenAsync(ResourceIdAzure, clientCredential).Result;

            return result.AccessToken;
        }

        private static StorageCredentials GetStorage(string accessToken)
        {
            TokenCredential tokenCredential = new TokenCredential(accessToken);

            StorageCredentials storageCredentials = new StorageCredentials(tokenCredential);

            return storageCredentials;
        }

        private static async Task<string> UploadFileAzure(byte[] file, string formatFile)
        {
            string caminhoArquivo = string.Empty;

            try
            {
                CloudBlobClient blobClient = GetServiceClient();

                //creating container
                CloudBlobContainer container = blobClient.GetContainerReference(ContainerNameAzure);

                string containerName = string.Empty;

                await container.CreateIfNotExistsAsync();

                containerName = Guid.NewGuid().ToString();

                //Nome do Arquivo que será subido
                CloudBlockBlob blobFile = container.GetBlockBlobReference($"{containerName}.{formatFile}");

                //Upload do arquivo pelo array
                await blobFile.UploadFromByteArrayAsync(file, 0, file.Length);

                //ToDo: (DACF) - Retirar o Download. O mesmo está aqui apenas para teste.
                await blobFile.DownloadToFileAsync($"C:\\temp\\Azure\\{containerName}.{formatFile}", FileMode.OpenOrCreate);

                caminhoArquivo = blobFile.Uri.ToString();
            }
            catch (Exception)
            {
                throw;
            }

            return caminhoArquivo;
        }

        private static async Task<string> DownloadFileAzure(string filePathAzure)
        {
            try
            {
                CloudBlobClient blobClient = GetServiceClient();

                //creating container
                CloudBlobContainer container = blobClient.GetContainerReference(ContainerNameAzure);

                await container.CreateIfNotExistsAsync();

                //Nome do Arquivo que será subido
                CloudBlockBlob blobFile = container.GetBlockBlobReference($"{filePathAzure}");

                var filePath = $"{SourceFolder}\\{ blobFile.Name}";

                await blobFile.DownloadToFileAsync(filePath, FileMode.OpenOrCreate);

                return filePath;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static CloudBlobClient GetServiceClient()
        {
            //Get Credential
            string accessToken = GetCredential();

            //Get Storage
            StorageCredentials storageCredentials = GetStorage(accessToken);

            //Get Blob
            CloudBlockBlob blob = new CloudBlockBlob(new Uri($"{BlobStorageAzure}/{ContainerNameAzure}"), storageCredentials);

            CloudBlobClient blobClient = blob.ServiceClient;
            return blobClient;
        }
    }
}
