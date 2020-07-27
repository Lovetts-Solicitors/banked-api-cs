// This code is copyright (c) 2020 Lovetts Solicitors (https://lovetts.co.uk) and provided under the MIT license (https://choosealicense.com/licenses/mit/) . You must ensure that this header and the separate license text is included in any software incorporating this file.

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;

namespace Banked {

    public class BankedAPIHelper {
        public const string BANKED_API_URL_PAYMENT_SESSION = @"https://api.banked.com/v2/payment_sessions/";
        public const string BANKED_API_URL_BANK_ACCOUNT = @"https://api.banked.com/v2/bank_accounts/";
        public const string BANKED_API_URL_PROVIDER = @"https://api.banked.com/v2/providers/";

        /// <summary>
        /// Default constructor for Banked API, passing in API credentials
        /// </summary>
        /// <param name="strPublicKey">Public key from the Banked console</param>
        /// <param name="strPrivateKey">Secret/Private key from the Banked console</param>
        public BankedAPIHelper(string strPublicKey, string strPrivateKey) {
            mBankedAPIPublicKey = strPublicKey;
            mBankedAPIPrivateKey = strPrivateKey;
        }

        protected string mBankedAPIPublicKey;
        /// <summary>
        /// Public API key
        /// </summary>
        public string BankedAPIPublicKey {
            get { return mBankedAPIPublicKey; }
            set { mBankedAPIPublicKey = value; }
        }

        protected string mBankedAPIPrivateKey;
        /// <summary>
        /// Secret/Private API key
        /// </summary>
        public string BankedAPIPrivateKey {
            get { return mBankedAPIPrivateKey; }
            set { mBankedAPIPrivateKey = value; }
        }

        /// <summary>
        /// Create a new payment session using the Banked API
        /// </summary>
        /// <param name="mSessionToCreate">BankedPaymentSession object representing the payment session to create using the API</param>
        /// <returns>BankedPaymentSession object representing the newly created payment session</returns>
        public BankedPaymentSession CreatePaymentSession(BankedPaymentSession mSessionToCreate) {
            JsonSerializerSettings mJsonSettings = new JsonSerializerSettings();
            mJsonSettings.NullValueHandling = NullValueHandling.Ignore;
            string mPaymentSessionData = JsonConvert.SerializeObject(mSessionToCreate, mJsonSettings);
            using (WebClient mBankedClient = new WebClient()) {
                mBankedClient.Headers.Add("content-type", "application/json");
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(BankedAPIPublicKey + ":" + BankedAPIPrivateKey));
                mBankedClient.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
                try {
                    string mResult = mBankedClient.UploadString(BANKED_API_URL_PAYMENT_SESSION, mPaymentSessionData);
                    BankedPaymentSession mReturnedSession = JsonConvert.DeserializeObject<BankedPaymentSession>(mResult);
                    return mReturnedSession;
                } catch (WebException wex) {
                    HttpWebResponse responseDetails = (System.Net.HttpWebResponse)wex.Response;
                    switch (responseDetails.StatusCode) {
                        case ((HttpStatusCode)422):
                        case HttpStatusCode.BadRequest:
                            using (StreamReader r = new StreamReader(responseDetails.GetResponseStream())) {
                                string mErrorDetail = r.ReadToEnd();
                                BankedErrors bErrors = JsonConvert.DeserializeObject<BankedErrors>(mErrorDetail);
                                string mErrorDetails = "";
                                foreach (BankedError bError in bErrors.Errors) {
                                    mErrorDetails += bError.Code + ":" + bError.Message + ",";
                                }
                                throw new BankedException(mErrorDetails, wex);
                            }
                            break;
                        default:
                            using (StreamReader r = new StreamReader(responseDetails.GetResponseStream())) {
                                string mErrorDetail = r.ReadToEnd();
                                throw new BankedException("Unknown HTTP error when attempting to create payment session: " + mErrorDetail, wex);
                            }
                            break;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Delete a payment session on the Banked API
        /// </summary>
        /// <param name="mPaymentId">String ID of the payment session to delete</param>
        /// <returns>Boolean to indicate success - true if deleted, false if not deleted or there was a problem</returns>
        public bool DeletePaymentSession(string mPaymentId) {
            if (mPaymentId != null && mPaymentId != "") {
                using (WebClient mBankedClient = new WebClient()) {
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(BankedAPIPublicKey + ":" + BankedAPIPrivateKey));
                    mBankedClient.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
                    try {
                        string mDownloadUrl = BANKED_API_URL_PAYMENT_SESSION + mPaymentId;
                        mBankedClient.UploadValues(mDownloadUrl, "DELETE", new System.Collections.Specialized.NameValueCollection());
                        return true;
                    } catch (WebException wex) {
                        HttpWebResponse responseDetails = (System.Net.HttpWebResponse)wex.Response;
                        switch (responseDetails.StatusCode) {
                            case ((HttpStatusCode)404):
                                return false;
                                break;
                            case HttpStatusCode.Unauthorized:
                                throw new BankedAuthException("Invalid credentials - check public key and secret key", wex);
                                break;
                            default:
                                throw new BankedException("Unknown HTTP error when attempting to create payment session", wex);
                                break;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Fetch an existing banked payment session object referenced by the payment ID
        /// </summary>
        /// <param name="mPaymentId">String ID of the payment session to fetch</param>
        /// <returns>BankedPaymentSession object representing the payment session retrieved from the API. Returns null if the payment ID is invalid or cannot be found by the API</returns>
        public BankedPaymentSession GetPaymentSession(string mPaymentId) {
            if (mPaymentId != null && mPaymentId != "") {
                using (WebClient mBankedClient = new WebClient()) {
                    string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(BankedAPIPublicKey + ":" + BankedAPIPrivateKey));
                    mBankedClient.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
                    try {
                        string mDownloadUrl = BANKED_API_URL_PAYMENT_SESSION + mPaymentId;
                        string mResult = mBankedClient.DownloadString(mDownloadUrl);
                        BankedPaymentSession mReturnedSession = JsonConvert.DeserializeObject<BankedPaymentSession>(mResult);
                        return mReturnedSession;
                    } catch (WebException wex) {
                        HttpWebResponse responseDetails = (System.Net.HttpWebResponse)wex.Response;
                        switch (responseDetails.StatusCode) {
                            case ((HttpStatusCode)404):
                                return null;
                                break;
                            case HttpStatusCode.Unauthorized:
                                throw new BankedAuthException("Invalid credentials - check public key and secret key", wex);
                                break;
                            default:
                                throw new BankedException("Unknown HTTP error when attempting to create payment session", wex);
                                break;
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Fetch all available Banked providers
        /// </summary>
        /// <returns>Array of BankedProvider objects representing the available providers (banks or financial institutions) supported by Banked along with their current status</returns>
        public BankedProvider[] GetProviders() {
            using (WebClient mBankedClient = new WebClient()) {
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(BankedAPIPublicKey + ":" + BankedAPIPrivateKey));
                mBankedClient.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
                try {
                    string mDownloadUrl = BANKED_API_URL_PROVIDER;
                    string mResult = mBankedClient.DownloadString(mDownloadUrl);
                    BankedProvider[] mReturnedSession = JsonConvert.DeserializeObject<BankedProvider[]>(mResult);
                    return mReturnedSession;
                } catch (WebException wex) {
                    HttpWebResponse responseDetails = (System.Net.HttpWebResponse)wex.Response;
                    switch (responseDetails.StatusCode) {
                        case HttpStatusCode.Unauthorized:
                            throw new BankedAuthException("Invalid credentials - check public key and secret key", wex);
                            break;
                        default:
                            throw new BankedException("Unknown HTTP error when attempting to fetch providers", wex);
                            break;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Fetch all available bank accounts wired up to Banked
        /// </summary>
        /// <returns>Array of BankedBankAccount objects representing the available bank accounts associated with this account in Banked</returns>
        public BankedBankAccount[] GetBankAccounts() {
            using (WebClient mBankedClient = new WebClient()) {
                string credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes(BankedAPIPublicKey + ":" + BankedAPIPrivateKey));
                mBankedClient.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;
                try {
                    string mDownloadUrl = BANKED_API_URL_BANK_ACCOUNT;
                    string mResult = mBankedClient.DownloadString(mDownloadUrl);
                    BankedBankAccount[] mReturnedSession = JsonConvert.DeserializeObject<BankedBankAccount[]>(mResult);
                    return mReturnedSession;
                } catch (WebException wex) {
                    HttpWebResponse responseDetails = (System.Net.HttpWebResponse)wex.Response;
                    throw new BankedException("Unknown HTTP error when attempting to fetch bank accounts", wex);
                }
            }
            return null;
        }
    }

    /// <summary>
    /// Represents a Banked 'Payee' object
    /// </summary>
    public class BankedPaymentSession {
        /// <summary>
        /// Default constructor for Payment Session object
        /// </summary>
        public BankedPaymentSession() { }
        /// <summary>
        /// Full constructor for Payment Session object
        /// </summary>
        /// <param name="strSuccessUrl">URL to redirect to on success</param>
        /// <param name="strErrorUrl">URL to redirect to if there are any errors during the payment process</param>
        /// <param name="lineItems">Array of line items making up the payment</param>
        /// <param name="payee">Details of the payee whom the payment is to be made to</param>
        /// <param name="strReference">Custom reference included with the bank transfer (appears on the payer's bank statement) - max 18 characters</param>
        /// <param name="payer">Details of person making the payment request. Optional if using Banked Checkout in which case pass in null/Nothing as the value</param>
        /// <param name="blnEmailReceipt">True if a receipt should be automatically emailed to the payer (assuming payer.email has been provided)</param>
        public BankedPaymentSession(string strSuccessUrl, string strErrorUrl, BankedLineItem[] lineItems, BankedPayee payee, string strReference, BankedPayer payer, bool blnEmailReceipt) {
            mSuccessUrl = strSuccessUrl;
            mErrorUrl = strErrorUrl;
            mLineItems = lineItems;
            mPayee = payee;
            mReference = strReference;
            mPayer = payer;
            mEmailReceipt = blnEmailReceipt;
        }

        protected string mId;
        /// <summary>
        /// URL to redirect to on success. This cannot be a relative URL.
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string Id {
            get { return mId; }
            set { mId = value; }
        }

        protected string mSuccessUrl;
        /// <summary>
        /// URL to redirect to on success. This cannot be a relative URL.
        /// </summary>
        [JsonProperty(PropertyName = "success_url")]
        public string SuccessUrl {
            get { return mSuccessUrl; }
            set { mSuccessUrl = value; }
        }

        protected string mErrorUrl;
        /// <summary>
        /// URL to redirect to if there is an error during the Banked payment process. This cannot be a relative URL.
        /// </summary>
        [JsonProperty(PropertyName = "error_url")]
        public string ErrorUrl {
            get { return mErrorUrl; }
            set { mErrorUrl = value; }
        }

        protected BankedLineItem[] mLineItems;
        /// <summary>
        /// Payee bank account sort code
        /// </summary>
        [JsonProperty(PropertyName = "line_items")]
        public BankedLineItem[] LineItems {
            get { return mLineItems; }
            set { mLineItems = value; }
        }

        protected BankedPayee mPayee;
        /// <summary>
        /// Payee bank account sort code
        /// </summary>
        [JsonProperty(PropertyName = "payee")]
        public BankedPayee Payee {
            get { return mPayee; }
            set { mPayee = value; }
        }

        protected string mReference;
        /// <summary>
        /// Payee bank account sort code
        /// </summary>
        [JsonProperty(PropertyName = "reference")]
        public string Reference {
            get { return mReference; }
            set { mReference = value; }
        }

        protected BankedPayer mPayer;
        /// <summary>
        /// Payee bank account sort code
        /// </summary>
        [JsonProperty(PropertyName = "payer")]
        public BankedPayer Payer {
            get { return mPayer; }
            set { mPayer = value; }
        }

        protected string mSessionState;
        /// <summary>
        /// Current state of payment session
        /// </summary>
        [JsonProperty(PropertyName = "state")]
        public string SessionState {
            get { return mSessionState; }
            set { mSessionState = value; }
        }

        protected string mPaymentURL;
        /// <summary>
        /// URL to direct customer to for payment
        /// </summary>
        [JsonProperty(PropertyName = "url")]
        public string PaymentURL {
            get { return mPaymentURL; }
            set { mPaymentURL = value; }
        }

        protected decimal mTotalAmount;
        /// <summary>
        /// Total amount being requested by this payment session
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public decimal TotalAmount {
            get { return mTotalAmount; }
            set { mTotalAmount = value; }
        }

        protected DateTime? mCreatedAt;
        /// <summary>
        /// Date/Time the payment session was created
        /// </summary>
        [JsonProperty(PropertyName = "created_at")]
        [JsonConverter(typeof(BankedDateTimeConverter))]
        public DateTime? CreatedAt {
            get { return mCreatedAt; }
            set { mCreatedAt = value; }
        }

        protected string mEndToEndID;
        /// <summary>
        /// ID for the entire transaction journey
        /// </summary>
        [JsonProperty(PropertyName = "end_to_end_id")]
        public string EndToEndID {
            get { return mEndToEndID; }
            set { mEndToEndID = value; }
        }

        protected bool mLiveMode;
        /// <summary>
        /// Payee bank account sort code
        /// </summary>
        [JsonProperty(PropertyName = "live")]
        public bool LiveMode {
            get { return mLiveMode; }
            set { mLiveMode = value; }
        }

        protected bool mEmailReceipt;
        /// <summary>
        /// Payee bank account sort code
        /// </summary>
        [JsonProperty(PropertyName = "email_receipt")]
        public bool EmailReceipt {
            get { return mEmailReceipt; }
            set { mEmailReceipt = value; }
        }
    }

    /// <summary>
    /// Represents a Banked 'Payee' object
    /// </summary>
    public class BankedPayee {

        /// <summary>
        /// Default constructor for Payee object
        /// </summary>
        public BankedPayee() { }
        /// <summary>
        /// Full constructor for Payee object
        /// </summary>
        /// <param name="strName">Payee name (e.g "Joe Bloggs")</param>
        /// <param name="strAccountNumber">Payee account number (no spaces - e.g 12345678)</param>
        /// <param name="strSortCode">Payee sort code (do not include hyphens - e.g 123456)</param>
        public BankedPayee(string strName, string strAccountNumber, string strSortCode) {
            mName = strName;
            mAccountNumber = strAccountNumber;
            mSortCode = strSortCode;
        }

        protected string mName;
        /// <summary>
        /// Payee name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name {
            get { return mName; }
            set { mName = value; }
        }

        protected string mAccountNumber;
        /// <summary>
        /// Payee bank account number
        /// </summary>
        [JsonProperty(PropertyName = "account_number")]
        public string AccountNumber {
            get { return mAccountNumber; }
            set { mAccountNumber = value; }
        }

        protected string mSortCode;
        /// <summary>
        /// Payee bank account sort code
        /// </summary>
        [JsonProperty(PropertyName = "sort_code")]
        public string SortCode {
            get { return mSortCode; }
            set { mSortCode = value; }
        }
    }

    /// <summary>
    /// Represents a Banked 'Payer' object
    /// </summary>
    public class BankedPayer {

        /// <summary>
        /// Default constructor for Payer object
        /// </summary>
        public BankedPayer() { }
        /// <summary>
        /// Full constructor for Payer object
        /// </summary>
        /// <param name="strName">Payer name (e.g "Joe Bloggs")</param>
        /// <param name="strEmailAddress">Payer email address</param>
        public BankedPayer(string strName, string strEmailAddress) {
            mName = strName;
            mEmailAddress = strEmailAddress;
        }

        protected string mName;
        /// <summary>
        /// Payee name
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name {
            get { return mName; }
            set { mName = value; }
        }

        protected string mEmailAddress;
        /// <summary>
        /// Payee bank account number
        /// </summary>
        [JsonProperty(PropertyName = "email")]
        public string EmailAddress {
            get { return mEmailAddress; }
            set { mEmailAddress = value; }
        }
    }

    /// <summary>
    /// Represents a Banked 'Line Item' object
    /// </summary>
    public class BankedLineItem {

        /// <summary>
        /// Default constructor for Banked Line Item object
        /// </summary>
        public BankedLineItem() { }
        /// <summary>
        /// Full constructor for Banked Line Item object
        /// </summary>
        /// <param name="strName">Name of line item (max 256 chars)</param>
        /// <param name="strDescription">Optional line item description</param>
        /// <param name="intAmount">Amount of line item in 1/100 of currency (e.g 12345 is £123.45)</param>
        /// <param name="strCurrency">Currency of the line item</param>
        /// <param name="intQuantity">Quantity of the line item</param>
        public BankedLineItem(string strName, string strDescription, int intAmount, string strCurrency, int intQuantity) {
            mName = strName;
            mDescription = strDescription;
            mAmount = intAmount;
            mCurrency = strCurrency;
            mQuantity = intQuantity;
        }

        protected string mName;
        /// <summary>
        /// Name of the line item (max 256 characters)
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name {
            get { return mName; }
            set { mName = value; }
        }

        protected string mDescription;
        /// <summary>
        /// Description of line item (optional)
        /// </summary>
        [JsonProperty(PropertyName = "description")]
        public string Description {
            get { return mDescription; }
            set { mDescription = value; }
        }

        protected int mAmount;
        /// <summary>
        /// Value (amount) of line item. Note that the Banked API only supports amounts in pence (e.g 1/100 of the amount) hence this is an integer field
        /// </summary>
        [JsonProperty(PropertyName = "amount")]
        public int Amount {
            get { return mAmount; }
            set { mAmount = value; }
        }

        protected string mCurrency;
        /// <summary>
        /// Currency of line item as ISO4271 currency code (currently only supports GBP)
        /// </summary>
        [JsonProperty(PropertyName = "currency")]
        public string Currency {
            get { return mCurrency; }
            set { mCurrency = value; }
        }

        protected int mQuantity;
        /// <summary>
        /// Quantity of the line item
        /// </summary>
        [JsonProperty(PropertyName = "quantity")]
        public int Quantity {
            get { return mQuantity; }
            set { mQuantity = value; }
        }
    }

    /// <summary>
    /// Represents a Banked 'Provider' object indicating a bank or financial institution supported by Banked
    /// </summary>
    public class BankedProvider {

        /// <summary>
        /// Default constructor for Banked 'provider' object
        /// </summary>
        public BankedProvider() { }

        protected string mID;
        /// <summary>
        /// Unique ID for provider/bank
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string ID {
            get { return mID; }
            set { mID = value; }
        }

        protected string mName;
        /// <summary>
        /// Name of the line item (max 256 characters)
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string Name {
            get { return mName; }
            set { mName = value; }
        }

        protected string mStatus;
        /// <summary>
        /// Current status of provider ("AVAILABLE" is the expected state)
        /// </summary>
        [JsonProperty(PropertyName = "status")]
        public string Status {
            get { return mStatus; }
            set { mStatus = value; }
        }

        protected bool mSupportsBatch;
        /// <summary>
        /// Indicates if the provider supports batch payments
        /// </summary>
        [JsonProperty(PropertyName = "supports_batch")]
        public bool SupportsBatch {
            get { return mSupportsBatch; }
            set { mSupportsBatch = value; }
        }
    }

    /// <summary>
    /// Represents a Banked 'Bank Account' object indicating a bank account that has been wired up to the Banked API
    /// </summary>
    public class BankedBankAccount {

        /// <summary>
        /// Default constructor for Banked 'bank account' object
        /// </summary>
        public BankedBankAccount() { }

        protected string mID;
        /// <summary>
        /// Unique ID for bank account
        /// </summary>
        [JsonProperty(PropertyName = "id")]
        public string ID {
            get { return mID; }
            set { mID = value; }
        }

        protected string mAccountName;
        /// <summary>
        /// Name of bank account
        /// </summary>
        [JsonProperty(PropertyName = "name")]
        public string AccountName {
            get { return mAccountName; }
            set { mAccountName = value; }
        }

        protected string mAccountNumber;
        /// <summary>
        /// Bank account number
        /// </summary>
        [JsonProperty(PropertyName = "account_number")]
        public string AccountNumber {
            get { return mAccountNumber; }
            set { mAccountNumber = value; }
        }

        protected string mAccountType;
        /// <summary>
        /// Type of bank account (e.g personal or business)
        /// </summary>
        [JsonProperty(PropertyName = "account_type")]
        public string AccountType {
            get { return mAccountType; }
            set { mAccountType = value; }
        }

        protected string mAccountSubType;
        /// <summary>
        /// Sub-type of bank account (e.g current, savings, etc)
        /// </summary>
        [JsonProperty(PropertyName = "account_sub_type")]
        public string AccountSubType {
            get { return mAccountSubType; }
            set { mAccountSubType = value; }
        }

        protected string mSortCode;
        /// <summary>
        /// Sort code of bank account
        /// </summary>
        [JsonProperty(PropertyName = "sort_code")]
        public string SortCode {
            get { return mSortCode; }
            set { mSortCode = value; }
        }

        protected string mAccountCurrency;
        /// <summary>
        /// Default currency of bank account
        /// </summary>
        [JsonProperty(PropertyName = "currency")]
        public string AccountCurrency {
            get { return mAccountCurrency; }
            set { mAccountCurrency = value; }
        }

        protected DateTime? mConsentExpiresAt;
        /// <summary>
        /// Indicates when the consent granted to Banked to access this bank account will expire and will need to be re-granted
        /// </summary>
        [JsonProperty(PropertyName = "consent_expires_at")]
        public DateTime? ConsentExpiresAt {
            get { return mConsentExpiresAt; }
            set { mConsentExpiresAt = value; }
        }

        protected BankedProvider mProvider;
        /// <summary>
        /// Provider (bank or financial institution) of the bank account
        /// </summary>
        [JsonProperty(PropertyName = "provider")]
        public BankedProvider Provider {
            get { return mProvider; }
            set { mProvider = value; }
        }
    }

    /// <summary>
    /// Represents any error that can be returned from the Banked API
    /// </summary>
    [System.Serializable]
    public class BankedException : Exception {

        public BankedException() : base() { }
        public BankedException(string strMessage) : base(strMessage) { }
        public BankedException(string strMessage, Exception innerException) : base(strMessage, innerException) { }
        protected BankedException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Object representing array of errors returned by the Banked API
    /// </summary>
    public class BankedErrors {
        public BankedErrors() { }

        protected BankedError[] mErrors;
        /// <summary>
        /// Array of Banked 'Error' objects
        /// </summary>
        [JsonProperty(PropertyName = "errors")]
        public BankedError[] Errors {
            get { return mErrors; }
            set { mErrors = value; }
        }
    }

    /// <summary>
    /// Object representing an individual error returned by the Banked API
    /// </summary>
    public class BankedError {
        public BankedError() { }
        protected string mCode;
        /// <summary>
        /// Type of error
        /// </summary>
        [JsonProperty(PropertyName = "code")]
        public string Code {
            get { return mCode; }
            set { mCode = value; }
        }

        protected dynamic mSource;
        /// <summary>
        /// Location of error
        /// </summary>
        [JsonProperty(PropertyName = "source")]
        public dynamic Source {
            get { return mSource; }
            set { mSource = value; }
        }

        protected string mMessage;
        /// <summary>
        /// Detailed error message
        /// </summary>
        [JsonProperty(PropertyName = "title")]
        public string Message {
            get { return mMessage; }
            set { mMessage = value; }
        }

    }

    /// <summary>
    /// Represents any error that can be returned from the Banked API
    /// </summary>
    [System.Serializable]
    public class BankedAuthException : Exception {

        public BankedAuthException() : base() { }
        public BankedAuthException(string message) : base(message) { }
        public BankedAuthException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class BankedDateTimeConverter : DateTimeConverterBase {
        private const string Format = "dd-MM-yyyy HH:mm:ss UTC";

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
            writer.WriteValue(((DateTime)value).ToString(Format));
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) {
            if (reader.Value == null) {
                return null;
            }

            var s = reader.Value.ToString();
            DateTime result;
            if (DateTime.TryParseExact(s, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)) {
                return result;
            }

            return null;
        }
    }
}
