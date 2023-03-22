using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleSupplier.Models.UpdateSupplierBankData
{
    using System;
    using Newtonsoft.Json;

    public class UpdateSupplierAccountReturnModel
    {
        [JsonProperty("BankAccountNumber")]
        public string BankAccountNumber { get; set; }

        [JsonProperty("CountryCode")]
        public string CountryCode { get; set; }

        [JsonProperty("BankBranchIdentifier")]
        public string BankBranchIdentifier { get; set; }

        [JsonProperty("BankIdentifier")]
        public string BankIdentifier { get; set; }

        [JsonProperty("BankAccountId")]
        public string BankAccountId { get; set; }

        [JsonProperty("CurrencyCode")]
        public string CurrencyCode { get; set; }

        [JsonProperty("IBAN")]
        public string Iban { get; set; }

        [JsonProperty("CheckDigits")]
        public string CheckDigits { get; set; }

        [JsonProperty("AccountType")]
        public string AccountType { get; set; }

        [JsonProperty("AccountSuffix")]
        public string AccountSuffix { get; set; }

        [JsonProperty("AgencyLocationCode")]
        public string AgencyLocationCode { get; set; }

        [JsonProperty("AllowInternationalPaymentIndicator")]
        public string AllowInternationalPaymentIndicator { get; set; }

        [JsonProperty("SecondaryAccountReference")]
        public string SecondaryAccountReference { get; set; }

        [JsonProperty("StartDate")]
        public DateTimeOffset StartDate { get; set; }

        [JsonProperty("EndDate")]
        public DateTimeOffset EndDate { get; set; }

        [JsonProperty("BankAccountName")]
        public string BankAccountName { get; set; }

        [JsonProperty("AlternateAccountName")]
        public string AlternateAccountName { get; set; }

        [JsonProperty("BankBranchPartyIndicator")]
        public string BankBranchPartyIndicator { get; set; }

        [JsonProperty("BankName")]
        public string BankName { get; set; }

        [JsonProperty("BankNumber")]
        public string BankNumber { get; set; }

        [JsonProperty("BankBranchName")]
        public string BankBranchName { get; set; }

        [JsonProperty("BankBranchNumber")]
        public string BankBranchNumber { get; set; }

        [JsonProperty("BIC")]
        public string Bic { get; set; }

        [JsonProperty("VendorId")]
        public string VendorId { get; set; }

        [JsonProperty("PersonId")]
        public string PersonId { get; set; }

        [JsonProperty("Intent")]
        public string Intent { get; set; }

        [JsonProperty("PartyId")]
        public string PartyId { get; set; }

        [JsonProperty("links")]
        public UpdateSupplierBankLink[] Links { get; set; }
    }

    public class UpdateSupplierBankLink
    {
        [JsonProperty("rel")]
        public string Rel { get; set; }

        [JsonProperty("href")]
        public string Href { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("kind")]
        public string Kind { get; set; }

        [JsonProperty("properties", NullValueHandling = NullValueHandling.Ignore)]
        public UpdateSupplierBankProperties Properties { get; set; }
    }

    public class UpdateSupplierBankProperties
    {
        [JsonProperty("changeIndicator")]
        public string ChangeIndicator { get; set; }
    }

}

