using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Data.SqlTypes;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BankingApplication
{
    public class DatabaseAccess
    {
        static readonly HttpClient Client = new HttpClient();
        private DatabaseAccess()  
        {  
        }  
        private static DatabaseAccess instance = null;  
        public static DatabaseAccess Instance  
        {  
            get  
            {  
                if (instance == null)  
                {
                    instance = new DatabaseAccess();  
                }  
                return instance;  
            }  

        } 

        private static IConfigurationRoot Configuration { get; } =
            new ConfigurationBuilder().AddJsonFile("appsettings.json").Build(); 

        private static string ConnectionString { get; } = Configuration["ConnectionString"];
        private static SqlConnection conn = new SqlConnection (ConnectionString);
        private SqlDataReader read; 
          
        
        public int DbChk(string sproc, int? account = null)
        {
            SqlCommand cmd = new SqlCommand(sproc, conn);

            cmd.CommandType = CommandType.StoredProcedure;

            //Output Parameter
            cmd.Parameters.Add("@bool", SqlDbType.Bit).Direction = ParameterDirection.Output;

            if (account.HasValue)
            {
                cmd.Parameters.AddWithValue("@accountNo", account);
            }

                try
            {
                conn.Open();
                cmd.ExecuteNonQuery();
                int chkresponse = Convert.ToInt32(cmd.Parameters["@bool"].Value);
                return chkresponse;
            }
                catch (SqlException se)
            {
                Console.WriteLine("SQL Exception: {0}", se.Message);
                return 3;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                return 3;
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();

                }
            }
        }
        

        public async Task GetJson()
        {
            var cjson = await Client.GetStringAsync("https://coreteaching01.csit.rmit.edu.au/~e87149/wdt/services/customers/");
            var ljson = await Client.GetStringAsync("https://coreteaching01.csit.rmit.edu.au/~e87149/wdt/services/logins/");

            //Variable for setting datetime format for reading json
            var format = "dd/MM/yyyy hh:mm:ss tt";
            var dateTimeConverter = new IsoDateTimeConverter { DateTimeFormat = format };
            AccountConverter converter = new AccountConverter();

            //Deserialize json into list (Referenced from Web Development Tutorial 2 but with added date time converter)
            List<Customer> tmpList = JsonConvert.DeserializeObject<List<Customer>>(cjson, converter, dateTimeConverter);

            SqlCommand LoginCmd = new SqlCommand("dbo.InsertLogin", conn);
            LoginCmd.CommandType = CommandType.StoredProcedure;
            
            SqlParameter jsonparam = new SqlParameter("@json", ljson);
            LoginCmd.Parameters.Add(jsonparam);

            try
            {
                conn.Open();
                foreach (Customer c in tmpList)
                {
                    SqlCommand CustCmd = new SqlCommand("INSERT INTO CUSTOMER (CustomerID, Name, Address, City, Postcode)" +
                                                        " VALUES(@CustomerID, @Name, @Address, @City, @Postcode )", conn);
                    CustCmd.Parameters.AddWithValue("@CustomerID", c.CustomerId);
                    CustCmd.Parameters.AddWithValue("@Name", c.Name);
                    CustCmd.Parameters.AddWithValue("@Address", c.Address);
                    CustCmd.Parameters.AddWithValue("@City", c.City);
                    CustCmd.Parameters.AddWithValue("@PostCode", c.PostCode);
                    CustCmd.ExecuteNonQuery();
                    foreach (IAccount a in c.Accounts)
                    {
                        SqlCommand AccCmd = new SqlCommand("INSERT INTO ACCOUNT (AccountNumber, AccountType, CustomerID, Balance)" +
                                                           " VALUES (@AccountNumber, @AccountType, @CustomerID, @Balance)", conn);
                        AccCmd.Parameters.AddWithValue("@AccountNumber", a.AccountNumber);
                        AccCmd.Parameters.AddWithValue("@AccountType", a.GetType().Name[0]);
                        AccCmd.Parameters.AddWithValue("@CustomerID", a.CustomerId);
                        AccCmd.Parameters.AddWithValue("@Balance", a.Balance);
                        AccCmd.ExecuteNonQuery();
                        foreach(Transaction t in a.Transactions)
                        {
                            SqlCommand TranCmd = new SqlCommand("INSERT INTO [TRANSACTION] (TransactionType, AccountNumber, DestinationAccountNumber, Amount, TransactionTimeUtc)" +
                                                                " VALUES (@TransactionType, @AccountNumber, @DestinationAccountNumber, @Amount, @TransactionTimeUtc)", conn);
                            TranCmd.Parameters.AddWithValue("@TransactionType", "D");
                            TranCmd.Parameters.AddWithValue("@AccountNumber", a.AccountNumber);
                            TranCmd.Parameters.AddWithValue("@DestinationAccountNumber", a.AccountNumber);
                            TranCmd.Parameters.AddWithValue("@Amount", a.Balance);
                            TranCmd.Parameters.AddWithValue("@TransactionTimeUtc", t.TransactionTimeUtc);
                            TranCmd.ExecuteNonQuery();
                        }
                    }
                }
                LoginCmd.ExecuteNonQuery();
            }
            catch (SqlException se)
            {
                Console.WriteLine("SQL Exception: {0}", se.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
            finally
            {
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();

                }
            }

        }

        public void UpdateBalance(decimal amount, int accountNumber)
                {
                    try
                    {
                        conn.Open();

                        SqlCommand cmd = new SqlCommand("update account set balance = @balance where accountnumber = @accountNumber", conn);

                        cmd.Parameters.AddWithValue("@balance",amount);
                        cmd.Parameters.AddWithValue("@accountNumber",accountNumber);

                        int update = cmd.ExecuteNonQuery();

                    }
                    catch (SqlException se)
                    {
                        Console.WriteLine("SQL Exception: {0}", se.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: {0}", e.Message);
                    }
                    finally
                    {
                        if (read != null)
                        {
                            read.Close();
                        }

                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }

        public void InsertTransaction(Transaction t)
                {
                    try
                    {
                        conn.Open();

                        SqlCommand cmd = new SqlCommand("INSERT INTO [TRANSACTION] (TransactionType, AccountNumber, DestinationAccountNumber, Amount, TransactionTimeUtc)" +
                                                                " VALUES (@TransactionType, @AccountNumber, case when @DestinationAccountNumber = 0 then null else @DestinationAccountNumber end, @Amount, @TransactionTimeUtc)", conn);
                        cmd.Parameters.AddWithValue("@TransactionType", t.TransactionType);
                        cmd.Parameters.AddWithValue("@AccountNumber", t.AccountNumber);
                        cmd.Parameters.AddWithValue("@DestinationAccountNumber", t.DestinationAccountNumber);
                        cmd.Parameters.AddWithValue("@Amount", t.Amount);
                        cmd.Parameters.AddWithValue("@TransactionTimeUtc", t.TransactionTimeUtc);
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException se)
                    {
                        Console.WriteLine("SQL Exception: {0}", se.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: {0}", e.Message);
                    }
                    finally
                    {
                        if (read != null)
                        {
                            read.Close();
                        }

                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                }
        public (string,string,string,string) GetCustomerDetails(int customerId)
                {
                    string name = "";
                    string address = "";
                    string city = "";
                    string postcode = "";
                    try
                    {
                        conn.Open();

                        SqlCommand cmd = new SqlCommand("select * from customer where customerid = @customerId", conn);

                        cmd.Parameters.AddWithValue("@customerId",customerId);

                        read = cmd.ExecuteReader();

                        while(read.Read())
                        {
                            name = read.GetString(1);
                            address = read.GetString(2);
                            city = read.GetString(3);
                            postcode = read.GetString(4);
                        }
                    }
                    catch (SqlException se)
                    {
                        Console.WriteLine("SQL Exception: {0}", se.Message);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception: {0}", e.Message);
                    }
                    finally
                    {
                        if (read != null)
                        {
                            read.Close();
                        }

                        if (conn != null)
                        {
                            conn.Close();
                        }
                    }
                    return (name,address,city,postcode);
                }
        public (int,string) GetLoginDetails(string loginId)
        {
            string passwordhash = "";
            int customerId = 0;
            try
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand("select * from login where loginid = @loginid", conn);

                cmd.Parameters.AddWithValue("@loginid",loginId);

                read = cmd.ExecuteReader();

                while(read.Read())
                {
                    customerId = read.GetInt32(1);
                    passwordhash = read.GetString(2);
                }
            }
            catch (SqlException se)
            {
                Console.WriteLine("SQL Exception: {0}", se.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
            finally
            {
                if (read != null)
                {
                    read.Close();
                }

                if (conn != null)
                {
                    conn.Close();
                }
            }
            return (customerId,passwordhash);
        }



        public List<Account> GetAccountData(int customerId)
        {       
            int accountNumber = 0;
            decimal balance = 0;
            char accountType = 'q';
            List<IAccount> accounts = new List<IAccount>();
            try
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("select * from account where customerid = @customerid", conn);

                cmd.Parameters.AddWithValue("@customerid", customerId);

                read = cmd.ExecuteReader();

                while (read.Read())
                {
                    accountNumber = read.GetInt32("accountnumber");
                    accountType = read.GetString(1)[0];
                    balance = read.GetDecimal(3);

                    var account = AccountFactory.CreateAccount(accountNumber, accountType, customerId, balance);
                    accounts.Add(account);
                }

            }
            catch (SqlException se)
            {
                Console.WriteLine("SQL Exception: {0}", se.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
            finally
            {
                if (read != null)
                {
                    read.Close();
                }

                if (conn != null)
                {
                    conn.Close();
                }
            }
            
            return accounts;
        }

        public List<Transaction> GetTransactionData(int accountId)
        {       
            int transactionId;
            char transactionType;
            int accountNumber;
            int destinationAccountNumber;
            decimal amount;
            string comment;
            DateTime transactionTimeUtc; 
            List<Transaction> transactions = new List<Transaction>(); 
            try
            {
               conn.Open();
                SqlCommand cmd = new SqlCommand("select * from [transaction] where accountnumber = @accountId", conn);

                cmd.Parameters.AddWithValue("@accountid",accountId);

                read = cmd.ExecuteReader();

                while (read.Read())
                {
                    transactionId = read.GetInt32("transactionId");
                    accountNumber = read.GetInt32("accountNumber");
                    
                    if (!read.IsDBNull(3))
                        destinationAccountNumber = read.GetInt32("destinationAccountNumber");
                    else
                        destinationAccountNumber = 0;

                    transactionType = read.GetString(1)[0];
                    amount = read.GetDecimal(4);
                    
                    if (!read.IsDBNull(5))
                        comment = read.GetString(5);
                    else
                        comment = null;

                    transactionTimeUtc = read.GetDateTime(6);

                    Transaction transaction = new Transaction(){
                        TransactionId = transactionId,
                        TransactionType = transactionType,
                        AccountNumber = accountNumber,
                        Amount = amount,
                        Comment = comment,
                        DestinationAccountNumber = destinationAccountNumber,
                        TransactionTimeUtc = transactionTimeUtc
                    };
                    
                    transactions.Add(transaction);
                }
            }
            catch (SqlException se)
            {
                Console.WriteLine("SQL Exception: {0}", se.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
            }
            finally
            {
                if (read != null)
                {
                    read.Close();
                }

                if (conn != null)
                {
                    conn.Close();
                }
            }
            
            return transactions;
        }
    }
}