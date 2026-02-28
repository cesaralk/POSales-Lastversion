using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace POSales
{
    public partial class Cashier : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;

        int qty;
        string id;
        string price;

        string stitle = "Point Of Sales";
        // double USD_TO_LBP = 89500;
        // Default exchange rate (USD -> L.L). User can override from Cashier screen.
        double USD_TO_LBP = 89500;

        // Expose current exchange rate for other forms (e.g., Settle).
        public double ExchangeRateLBP => GetExchangeRateLBP();

        private double GetExchangeRateLBP()
        {
            try
            {
                if (txtRate == null) return USD_TO_LBP;

                string s = (txtRate.Text ?? "").Trim();
                // allow "89,500" or "89500"
                s = s.Replace(",", "").Replace(" ", "");

                if (double.TryParse(s, out double rate) && rate > 0)
                    return rate;
            }
            catch { }
            return USD_TO_LBP;
        }

        private void SetExchangeRateText(double rate)
        {
            if (txtRate == null) return;
            txtRate.Text = rate.ToString("#,##0");
        }

        private void RefreshTotalsUsingRate()
        {
            GetCartTotal();

            double rate = GetExchangeRateLBP();
            double saleTotalLBP = CurrentSaleUSD * rate;
            lblSaleTotalLBP.Text = "L.L " + saleTotalLBP.ToString("#,##0");
        }
        public double CurrentSaleUSD = 0;

        public Cashier()
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            GetTranNo();
            lblDate.Text = DateTime.Now.ToShortDateString();
            txtBarcode.KeyDown += txtBarcode_KeyDown;


            // Some barcode scanners send CR (KeyPress '\r') or TAB instead of Enter.
            // Handle KeyPress too so "qty + scan" works reliably.
            txtBarcode.KeyPress += txtBarcode_KeyPress;

            // Qty helper: allow quick multiply (e.g., set Qty then scan)
            txtQty.KeyPress += TxtQty_KeyPress;
            // Allow user to change exchange rate.
            // Rate textbox (USD -> L.L)
            // Load saved exchange rate (USD -> L.L)
            try
            {
                double saved = Properties.Settings.Default.ExchangeRateLBP;
                if (saved > 0) USD_TO_LBP = saved;
            }
            catch { }

            // Rate textbox (USD -> L.L)
            if (txtRate != null)
            {
                SetExchangeRateText(USD_TO_LBP);
                txtRate.KeyPress += TxtRate_KeyPress;
                txtRate.Leave += TxtRate_Leave;
                txtRate.TextChanged += TxtRate_TextChanged;
            }
        }

        private void TxtRate_KeyPress(object sender, KeyPressEventArgs e)
        {
            // allow digits, backspace, and optional separators
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != ',' && e.KeyChar != '.')
            {
                e.Handled = true;
                return;
            }
        }

        private void TxtRate_Leave(object sender, EventArgs e)
        {
            // Validate + format on leave
            double rate = GetExchangeRateLBP();
            USD_TO_LBP = rate;

            // SAVE persistently (stays until user changes again)
            try
            {
                Properties.Settings.Default.ExchangeRateLBP = rate;
                Properties.Settings.Default.Save();
            }
            catch { }

            SetExchangeRateText(rate);
            RefreshTotalsUsingRate();
        }

        private void TxtRate_TextChanged(object sender, EventArgs e)
        {
            // Live refresh while typing (only when parseable)
            if (txtRate == null) return;

            string s = (txtRate.Text ?? "").Trim().Replace(",", "").Replace(" ", "");
            if (double.TryParse(s, out double rate) && rate > 0)
            {
                USD_TO_LBP = rate;
                RefreshTotalsUsingRate();
            }
        }

        private void txtBarcode_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Handle scanners that send carriage return (\r) as the suffix.
            if (e.KeyChar == (char)Keys.Return)
            {
                e.Handled = true;
                HandleBarcodeSubmit();
            }
        }

        private void HandleBarcodeSubmit()
        {
            var raw = txtBarcode.Text.Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                txtBarcode.Clear();
                return;
            }

            // Support quick multiplication:
            //  - Type Qty in txtQty (e.g., 5) then scan barcode
            //  - Or type: 5*BARCODE  (or BARCODE*5) then press Enter
            if (TryParseQtyAndBarcode(raw, out int qtyToAdd, out string barcode))
            {
                ProcessBarcode(barcode, qtyToAdd);
            }
            else
            {
                ProcessBarcode(raw, GetScanQtyFallback());
            }

            txtBarcode.Clear();

            // Reset qty back to 1 for next scan (common POS behavior)
            txtQty.Text = "1";
            txtQty.SelectionStart = 0;
            txtQty.SelectionLength = txtQty.Text.Length;
        }

        private void TxtQty_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Digits + control keys only
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        private void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            // Many scanners send Enter, some send Tab.
            if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                e.SuppressKeyPress = true; // prevents beep sound
                HandleBarcodeSubmit();
            }
        }

        private int GetScanQtyFallback()
        {
            if (int.TryParse(txtQty.Text.Trim(), out int q) && q > 0) return q;
            return 1;
        }

        private bool TryParseQtyAndBarcode(string input, out int qtyToAdd, out string barcode)
        {
            qtyToAdd = 1;
            barcode = input;

            if (string.IsNullOrWhiteSpace(input)) return false;
            var s = input.Replace(" ", "");

            // Accept both '*' and 'x' as multiplier separators
            char sep = s.Contains('*') ? '*' : (s.IndexOf('x') >= 0 ? 'x' : (s.IndexOf('X') >= 0 ? 'X' : '\0'));
            if (sep == '\0') return false;

            var parts = s.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;

            // Pattern: qty*barcode
            if (int.TryParse(parts[0], out int q1) && q1 > 0)
            {
                qtyToAdd = q1;
                barcode = parts[1];
                return !string.IsNullOrWhiteSpace(barcode);
            }

            // Pattern: barcode*qty
            if (int.TryParse(parts[1], out int q2) && q2 > 0)
            {
                qtyToAdd = q2;
                barcode = parts[0];
                return !string.IsNullOrWhiteSpace(barcode);
            }

            return false;
        }

        private void ProcessBarcode(string barcode, int qtyToAdd)
        {
            if (string.IsNullOrEmpty(barcode)) return;
            if (qtyToAdd <= 0) qtyToAdd = 1;

            try
            {
                cn.Open();
                cm = new SqlCommand("SELECT * FROM tbProduct WHERE barcode=@barcode", cn);
                cm.Parameters.AddWithValue("@barcode", barcode);
                dr = cm.ExecuteReader();

                if (dr.Read())
                {
                    string _pcode = dr["pcode"].ToString();
                    double _price = double.Parse(dr["price"].ToString());

                    qty = int.Parse(dr["qty"].ToString()); // ✅ SET STOCK ON HAND
                    int _qty = qtyToAdd; // ✅ scanned qty (supports multiplication)

                    dr.Close();
                    cn.Close();

                    AddToCart(_pcode, _price, _qty);
                }

                else
                {
                    dr.Close();
                    cn.Close();
                    MessageBox.Show("Product not found!", "Point Of Sales", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, "Point Of Sales", MessageBoxButtons.OK, MessageBoxIcon.Warning);


            }
        }

        private void picClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Exit Application?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        public void slide(Button button)
        {
            panelSlide.BackColor = Color.White;
            panelSlide.Height = button.Height;
            panelSlide.Top = button.Top;
        }
        #region button
        private void btnNTran_Click(object sender, EventArgs e)
        {
            slide(btnNTran);
            GetTranNo();

        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            slide(btnSearch);
            LookUpProduct lookUp = new LookUpProduct(this);
            lookUp.LoadProduct();
            lookUp.ShowDialog();
        }

        private void btnDiscount_Click(object sender, EventArgs e)
        {
            slide(btnDiscount);
            Discount discount = new Discount(this);
            discount.lbId.Text = id;
            discount.txtTotalPrice.Text = price;
            discount.ShowDialog();
        }

        private void btnSettle_Click(object sender, EventArgs e)
        {
            slide(btnSettle);
            Settle settle = new Settle(this);
            settle.ShowDialog();
        }


        private void btnClear_Click(object sender, EventArgs e)
        {
            slide(btnClear);
            if (MessageBox.Show("Remove all items from cart?", "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                cn.Open();
                cm = new SqlCommand("Delete from tbCart where transno like'" + lblTranNo.Text + "'", cn);
                cm.ExecuteNonQuery();
                cn.Close();
                MessageBox.Show("All items has been successfully remove", "Remove item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                LoadCart();
            }
        }

        private void btnDSales_Click(object sender, EventArgs e)
        {
            slide(btnDSales);
            DailySale dailySale = new DailySale(new MainForm());
            dailySale.solduser = lblUsername.Text;
            dailySale.dtFrom.Enabled = false;
            dailySale.dtTo.Enabled = false;
            dailySale.cboCashier.Enabled = false;
            dailySale.cboCashier.Text = lblUsername.Text;
            dailySale.picClose.Visible = true;
            dailySale.lblTitle.Visible = true;
            dailySale.ShowDialog();
        }

        private void btnPass_Click(object sender, EventArgs e)
        {
            slide(btnPass);
            ChangePassword change = new ChangePassword(this);
            change.ShowDialog();
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            slide(btnLogout);
            if (dgvCash.Rows.Count > 0)
            {
                MessageBox.Show("Unable to logout. Please cancel the transaction.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (MessageBox.Show("Logout Application?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.Hide();
                Login login = new Login();
                login.ShowDialog();
            }
        }
        #endregion button

        public void LoadCart()
        {
            try
            {
                Boolean hascart = false;
                int i = 0;
                double total = 0;
                double discount = 0;
                dgvCash.Rows.Clear();
                cn.Open();
                cm = new SqlCommand("SELECT c.id, c.pcode, p.pdesc, c.price, c.qty, c.disc, c.total FROM tbCart AS c INNER JOIN tbProduct AS p ON c.pcode=p.pcode WHERE c.transno LIKE @transno and c.status LIKE 'Pending'", cn);
                cm.Parameters.AddWithValue("@transno", lblTranNo.Text);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {

                    i++;
                    total += Convert.ToDouble(dr["total"].ToString());
                    discount += Convert.ToDouble(dr["disc"].ToString());
                    dgvCash.Rows.Add(i, dr["id"].ToString(), dr["pcode"].ToString(), dr["pdesc"].ToString(), dr["price"].ToString(), dr["qty"].ToString(), dr["disc"].ToString(), double.Parse(dr["total"].ToString()).ToString("#,##0.00"));//
                    hascart = true;
                }
                dr.Close();
                cn.Close();
                CurrentSaleUSD = total;
                lblSaleTotal.Text = "$ " + total.ToString("#,##0.00");
                lblDiscount.Text = discount.ToString("#,##0.00");
                GetCartTotal();

                //   double saleTotalLBP = total * USD_TO_LBP;
                double saleTotalLBP = total * GetExchangeRateLBP();

                lblSaleTotalLBP.Text = "L.L " + saleTotalLBP.ToString("#,##0");



                if (hascart) { btnClear.Enabled = true; btnSettle.Enabled = true; btnDiscount.Enabled = true; }
                else { btnClear.Enabled = false; btnSettle.Enabled = false; btnDiscount.Enabled = false; }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle);
            }

        }

        public void GetCartTotal()
        {
            double discount = double.Parse(lblDiscount.Text);
            // double sales = double.Parse(lblSaleTotal.Text.Replace("$", "").Trim()) - discount;
            double sales = CurrentSaleUSD - discount;

            double vat = sales * 0.12;//VAT: 12% of VAT Payable (Output Tax less Input Tax)
            double vatable = sales - vat;

            lblVat.Text = vat.ToString("#,##0.00");
            lblVatable.Text = vatable.ToString("#,##0.00");
            lblDisplayTotal.Text = "$ " + sales.ToString("#,##0.00");


            // double totalLBP = sales * USD_TO_LBP;
            double totalLBP = sales * GetExchangeRateLBP();
            lblDisplayTotalLBP.Text = totalLBP.ToString("#,##0") + " L.L";


        }
        private void timer1_Tick(object sender, EventArgs e)
        {
            lblTimer.Text = DateTime.Now.ToString("hh:mm:ss tt");
        }

        public void GetTranNo()
        {
            try
            {
                string sdate = DateTime.Now.ToString("yyyyMMdd");
                int count;
                string transno;
                cn.Open();
                cm = new SqlCommand("SELECT TOP 1 transno FROM tbCart WHERE transno LIKE '" + sdate + "%' ORDER BY id desc", cn);
                dr = cm.ExecuteReader();
                dr.Read();
                if (dr.HasRows)
                {
                    transno = dr[0].ToString();
                    count = int.Parse(transno.Substring(8, 4));
                    lblTranNo.Text = sdate + (count + 1);
                }
                else
                {
                    transno = sdate + "1001";
                    lblTranNo.Text = transno;
                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {

                cn.Close();
                MessageBox.Show(ex.Message, stitle);

            }

        }

        // private void txtBarcode_TextChanged(object sender, EventArgs e)
        //{
        //try
        //{
        //    if (txtBarcode.Text == string.Empty) return;
        //    else
        //    {
        //        string _pcode;
        //        double _price;
        //        int _qty;
        //        cn.Open();
        //        cm = new SqlCommand("SELECT * FROM tbProduct WHERE barcode LIKE '" + txtBarcode.Text + "'", cn);
        //        dr = cm.ExecuteReader();
        //        dr.Read();
        //        if (dr.HasRows)
        //        {
        //            qty = int.Parse(dr["qty"].ToString());
        //            _pcode = dr["pcode"].ToString();
        //            _price = double.Parse(dr["price"].ToString());
        //            _qty = int.Parse(txtQty.Text);

        //            dr.Close();
        //            cn.Close();
        //            //insert to tbCart
        //            AddToCart(_pcode, _price, _qty);
        //        }
        //        dr.Close();
        //        cn.Close();
        //    }
        //}
        //catch (Exception ex)
        //{
        //    cn.Close();
        //    MessageBox.Show(ex.Message, stitle, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //}
        //  }

        public void AddToCart(string _pcode, double _price, int _qty)
        {
            try
            {
                string id = "";
                int cart_qty = 0;
                bool found = false;
                cn.Open();
                cm = new SqlCommand("Select * from tbCart Where transno = @transno and pcode = @pcode", cn);
                cm.Parameters.AddWithValue("@transno", lblTranNo.Text);
                cm.Parameters.AddWithValue("@pcode", _pcode);
                dr = cm.ExecuteReader();
                dr.Read();
                if (dr.HasRows)
                {
                    id = dr["id"].ToString();
                    cart_qty = int.Parse(dr["qty"].ToString());
                    found = true;
                }
                else found = false;
                dr.Close();
                cn.Close();

                if (found)
                {
                    // if (qty < (int.Parse(txtQty.Text) + cart_qty))
                    if (qty < (_qty + cart_qty))

                    {
                        MessageBox.Show("Unable to procced. Remaining quantity on hand is " + qty, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    cn.Open();
                    cm = new SqlCommand("Update tbCart set qty = (qty + " + _qty + ")Where id= '" + id + "'", cn);
                    cm.ExecuteReader();
                    cn.Close();
                    txtBarcode.SelectionStart = 0;
                    txtBarcode.SelectionLength = txtBarcode.Text.Length;
                    LoadCart();
                }
                else
                {
                    //if (qty < (int.Parse(txtQty.Text) + cart_qty))
                    if (qty < (_qty + cart_qty))

                    {
                        MessageBox.Show("Unable to procced. Remaining qty on hand is" + qty, "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    cn.Open();
                    cm = new SqlCommand("INSERT INTO tbCart(transno, pcode, price, qty, sdate, cashier)VALUES(@transno, @pcode, @price, @qty, @sdate, @cashier)", cn);
                    cm.Parameters.AddWithValue("@transno", lblTranNo.Text);
                    cm.Parameters.AddWithValue("@pcode", _pcode);
                    cm.Parameters.AddWithValue("@price", _price);
                    cm.Parameters.AddWithValue("@qty", _qty);
                    cm.Parameters.AddWithValue("@sdate", DateTime.Now);
                    cm.Parameters.AddWithValue("@cashier", lblUsername.Text);
                    cm.ExecuteNonQuery();
                    cn.Close();
                    LoadCart();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, stitle);
            }
        }

        private void dgvCash_SelectionChanged(object sender, EventArgs e)
        {
            int i = dgvCash.CurrentRow.Index;
            id = dgvCash[1, i].Value.ToString();
            price = dgvCash[7, i].Value.ToString();
        }

        private void dgvCash_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string colName = dgvCash.Columns[e.ColumnIndex].Name;


            if (colName == "Delete")
            {
                if (MessageBox.Show("Remove this item", "Remove item", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    dbcon.ExecuteQuery("Delete from tbCart where id like'" + dgvCash.Rows[e.RowIndex].Cells[1].Value.ToString() + "'");
                    MessageBox.Show("Items has been successfully remove", "Remove item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    LoadCart();
                }
            }
            else if (colName == "colAdd")
            {
                int i = 0;
                cn.Open();
                cm = new SqlCommand("SELECT SUM(qty) as qty FROM tbProduct WHERE pcode LIKE'" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "' GROUP BY pcode", cn);
                i = int.Parse(cm.ExecuteScalar().ToString());
                cn.Close();
                if (int.Parse(dgvCash.Rows[e.RowIndex].Cells[5].Value.ToString()) < i)
                {
                    dbcon.ExecuteQuery("UPDATE tbCart SET qty = qty + " + int.Parse(txtQty.Text) + " WHERE transno LIKE '" + lblTranNo.Text + "'  AND pcode LIKE '" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "'");
                    LoadCart();
                }
                else
                {
                    MessageBox.Show("Remaining qty on hand is " + i + "!", "Out of Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else if (colName == "colReduce")
            {
                int i = 0;
                cn.Open();
                cm = new SqlCommand("SELECT SUM(qty) as qty FROM tbCart WHERE pcode LIKE'" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "' GROUP BY pcode", cn);
                i = int.Parse(cm.ExecuteScalar().ToString());
                cn.Close();
                if (i > 1)
                {
                    dbcon.ExecuteQuery("UPDATE tbCart SET qty = qty - " + int.Parse(txtQty.Text) + " WHERE transno LIKE '" + lblTranNo.Text + "'  AND pcode LIKE '" + dgvCash.Rows[e.RowIndex].Cells[2].Value.ToString() + "'");
                    LoadCart();
                }
                else
                {
                    MessageBox.Show("Remaining qty on cart is " + i + "!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
        }

        public void Noti()
        {
            int i = 0;
            cn.Open();
            cm = new SqlCommand("SELECT * FROM vwCriticalItems", cn);
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                Alert alert = new Alert(new MainForm());
                alert.lblPcode.Text = dr["pcode"].ToString();
                alert.showAlert(i + ". " + dr["pdesc"].ToString() + " - " + dr["qty"].ToString());
            }
            dr.Close();
            cn.Close();
        }

        private void Cashier_Load(object sender, EventArgs e)
        {
            Noti();
            txtBarcode.Focus();

        }

        private void lblTotalLBP_Click(object sender, EventArgs e)
        {

        }
    }
}


