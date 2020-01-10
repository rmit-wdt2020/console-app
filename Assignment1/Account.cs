﻿using System;
namespace HelloWorldApplication
{
    public enum Type
    {
        Savings,
        Checking
    }

    public class Account
    {
        private int accountNumber;
        private Type accountType;
        private Customer customer;      
        private decimal balance;

        public int AccountNumber
        {
            get { return accountNumber; }
            set { accountNumber = value; }
        }

        public Type AccountType
        {
            get { return accountType; }
            set { accountType = value; }
        }

        public Customer Customer
        {
            get { return customer; }
            set { customer = value; }
        }

        public decimal Balance
        {
            get { return balance; }
            set { balance = value; }

        }

        public Account()
        {
        }

        public void withdraw(decimal amount)
        {
            if(balance >= amount ) 
            {
                balance = balance - amount;
                DatabaseAccess.Instance.updateBalance(balance,this.accountNumber);
                Console.WriteLine(balance);
            }
            else 
            {
                Console.WriteLine("Insufficient funds");
            }
        }

        public void deposit(decimal amount)
        {
            balance = balance + amount;
            DatabaseAccess.Instance.updateBalance(balance,this.accountNumber);
            Console.WriteLine(balance);
        }
    }
}


    //AccountNumber int not null,
    //AccountType char not null,
    //CustomerID int not null,
    //Balance money not null,
    //constraint PK_Account primary key (AccountNumber),
    //constraint FK_Account_Customer foreign key (CustomerID) references Customer (CustomerID),
    //constraint CH_Account_AccountType check (AccountType in ('C', 'S')),
    //constraint CH_Account_Balance check(Balance >= 0)