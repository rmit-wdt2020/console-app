﻿using SimpleHashing;

namespace BankingApplication
{
    //Class for handling password hashing and authentication
    public class Authentication
    {
        public Authentication()
        {
        }

        // Uses the static method verify in the PBKDF2 static class located in the simple hashing namespace
        // to verify user inputted password with stored password hash.
        public bool login(string hash, string password)
        {
            bool userValidated = false;
            
            if(hash != null && hash!="")
            {
                userValidated = PBKDF2.Verify(hash, password);
            }

            return userValidated;
        }
        
    }
}
