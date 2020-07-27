# banked-api-cs
C# implementation of the [Banked.com](https://banked.com) API . Full API reference documentation can be found at [https://developer.banked.com/docs] .

## What is Banked? 

Banked is a new digital payment system built around Open Banking, providing real-time account to account payments within the UK.

## Copyright and disclaimer

This is an unofficial API wrapper developed by [Lovetts Solicitors](https://lovetts.co.uk) and is not approved or endorsed by Banked. The Banked API, website and developer documentation are copyright &copy; 2020 Banked.com Ltd.

All Lovetts code is released under the [MIT](https://choosealicense.com/licenses/mit/) license.

## Requirements

Developed against Microsoft.NET Framework 4.0 . However the code is sufficiently generic that it should work with older versions 

Requires the [Newtonsoft JSON.NET](https://www.newtonsoft.com/json) package or library to be installed.

## Usage

Include the Banked.cs file in your project or compile the class into a DLL and include that in your project.

Within your own project instantiate the BankedAPIHelper class with your public and secret key details and then call the appropriate function. 

Some examples of usage are:

### Fetch list of providers

```using Banked;

BankedAPIHelper mAPI = new BankedAPIHelper("pk_test_xxxxxx", "sk_test_xxxxxx");
BankedProvider[] mProviders = mAPI.GetProviders();
foreach( BankedProvider mProvider in mProviders ) {
    System.Diagnostics.Debug.WriteLine(mProvider.Name);
}
```

### Create a new payment session against the sandbox bank account for £123.45 

```using Banked;

BankedAPIHelper mAPI = new BankedAPIHelper("pk_test_xxxxxx", "sk_test_xxxxxx");

string mSuccessUrl = "http://localhost/success/";
string mErrorUrl = "http://localhost/error/";
BankedPayee mPayee = new BankedPayee("Sandbox Payee", "12345678", "010203");
BankedPayer mPayer = new BankedPayer("Mr Joe Bloggs", "test@test.com");
BankedLineItem mLineItem = new BankedLineItem();
decimal mAmount = 123.45m;
int mAmountFraction = (int)(mAmount * 100);
mLineItem.Amount = mAmountFraction;
mLineItem.Quantity = (int)numQuantity.Value;
mLineItem.Name = "Test Line Item";
mLineItem.Currency = "GBP";
BankedLineItem[] mLineItems = new BankedLineItem[] { mLineItem };
BankedPaymentSession mSession = new BankedPaymentSession(mSuccessUrl, mErrorUrl, mLineItems, mPayee, "test-reference", mPayer, true);
BankedPaymentSession mNewSession = mAPI.CreatePaymentSession(mSession);
System.Diagnostics.Debug.WriteLine(mNewSession.Id);
```

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
