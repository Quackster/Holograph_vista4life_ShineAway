using System;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

namespace Holo.Data.Repositories
{
    /// <summary>
    /// Provides data access operations for voucher-related database queries.
    /// All queries use parameterized SQL to prevent SQL injection.
    /// </summary>
    public class VoucherDataAccess : BaseDataAccess
    {
        /// <summary>
        /// Checks if a voucher code exists.
        /// </summary>
        public bool VoucherExists(string voucherCode)
        {
            string query = "SELECT voucher FROM vouchers WHERE voucher = @voucherCode LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@voucherCode", voucherCode)
            };
            return RecordExists(query, parameters);
        }

        /// <summary>
        /// Gets the credit amount for a voucher code.
        /// </summary>
        public int GetVoucherCredits(string voucherCode)
        {
            string query = "SELECT credits FROM vouchers WHERE voucher = @voucherCode LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@voucherCode", voucherCode)
            };
            return ExecuteScalarInt(query, parameters);
        }

        /// <summary>
        /// Redeems (deletes) a voucher code.
        /// </summary>
        public bool RedeemVoucher(string voucherCode)
        {
            string query = "DELETE FROM vouchers WHERE voucher = @voucherCode LIMIT 1";
            var parameters = new[]
            {
                new MySqlParameter("@voucherCode", voucherCode)
            };
            return ExecuteNonQuery(query, parameters);
        }
    }
}
