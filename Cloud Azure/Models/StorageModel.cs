using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Cloud_Azure.Models
{
    public class StorageModel
    {
        public int Id { get; set; }
        public string UrlArquivo { get; set; }
        public string NomeArquivo { get; set; }
    }
}