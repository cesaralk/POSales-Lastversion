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
    public partial class ProductStockIn : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        string stitle = "Point Of Sales";
        StockIn stockIn;
        public ProductStockIn(StockIn stk)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            stockIn = stk;
            LoadProduct();
        }

        public int AskQuantity()
        {
            Form f = new Form();
            f.Width = 250;
            f.Height = 140;
            f.Text = "Enter Quantity";
            f.StartPosition = FormStartPosition.CenterParent;
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.MaximizeBox = false;
            f.MinimizeBox = false;

            Label lbl = new Label();
            lbl.Text = "Quantity:";
            lbl.Location = new Point(30, 20);
            lbl.AutoSize = true;

            TextBox txt = new TextBox();
            txt.Text = "1";
            txt.Location = new Point(30, 45);
            txt.Width = 170;

            Button btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Location = new Point(80, 80);
            btnOk.DialogResult = DialogResult.OK;

            f.Controls.Add(lbl);
            f.Controls.Add(txt);
            f.Controls.Add(btnOk);
            f.AcceptButton = btnOk;

            if (f.ShowDialog() == DialogResult.OK)
            {
                int qty;
                if (int.TryParse(txt.Text, out qty) && qty > 0)
                    return qty;
            }

            return 1; // default if invalid
        }
      
        public void addStockInWithQty(string pcode, int qty)
        {
            try
            {
                cn.Open();

                // Try update first
                cm = new SqlCommand(
                    "UPDATE tbStockIn SET qty = qty + @qty " +
                    "WHERE refno=@refno AND pcode=@pcode", cn);

                cm.Parameters.AddWithValue("@qty", qty);
                cm.Parameters.AddWithValue("@refno", stockIn.txtRefNo.Text);
                cm.Parameters.AddWithValue("@pcode", pcode);

                int rowsAffected = cm.ExecuteNonQuery();

                // If no row updated, insert new
                if (rowsAffected == 0)
                {
                    cm = new SqlCommand(
                        "INSERT INTO tbStockIn (refno, pcode, qty, sdate, stockinby, supplierid) " +
                        "VALUES (@refno, @pcode, @qty, @sdate, @stockinby, @supplierid)", cn);

                    cm.Parameters.AddWithValue("@refno", stockIn.txtRefNo.Text);
                    cm.Parameters.AddWithValue("@pcode", pcode);
                    cm.Parameters.AddWithValue("@qty", qty);
                    cm.Parameters.AddWithValue("@sdate", stockIn.dtStockIn.Value);
                    cm.Parameters.AddWithValue("@stockinby", stockIn.txtStockInBy.Text);
                    cm.Parameters.AddWithValue("@supplierid", stockIn.lblId.Text);

                    cm.ExecuteNonQuery();
                }

                cn.Close();
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message, stitle);
            }
        }


        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void LoadProduct()
        {
            int i = 0;
            dgvProduct.Rows.Clear();
            cm = new SqlCommand("SELECT pcode, pdesc, qty FROM tbProduct WHERE pdesc LIKE '%" + txtSearch.Text + "%'", cn);
            cn.Open();
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                dgvProduct.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString());
            }
            dr.Close();
            cn.Close();
        }

        //private void dgvProduct_CellContentClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    string colName = dgvProduct.Columns[e.ColumnIndex].Name;
        //    if (colName == "Select")
        //    {
        //        if (string.IsNullOrWhiteSpace(stockIn.txtStockInBy.Text))
        //        {
        //            MessageBox.Show("Please enter stock in by name first.", stitle,
        //                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //            stockIn.txtStockInBy.Focus();
        //            return;
        //        }


        //        if (MessageBox.Show("Add this item?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        //        {
        //            addStockIn(dgvProduct.Rows[e.RowIndex].Cells[1].Value.ToString());
        //            MessageBox.Show("Successfully added", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);

        //        }
        //    }
        //}

        //public void addStockIn(string pcode)
        //{
        //    try
        //    {
        //        cn.Open();
        //        cm = new SqlCommand("INSERT INTO tbStockIn (refno, pcode, sdate, stockinby, supplierid)VALUES (@refno, @pcode, @sdate, @stockinby, @supplierid)", cn);
        //        cm.Parameters.AddWithValue("@refno", stockIn.txtRefNo.Text);
        //        cm.Parameters.AddWithValue("@pcode", pcode);
        //        cm.Parameters.AddWithValue("@sdate", stockIn.dtStockIn.Value);
        //        cm.Parameters.AddWithValue("@stockinby", stockIn.txtStockInBy.Text);
        //        cm.Parameters.AddWithValue("@supplierid", stockIn.lblId.Text);
        //        cm.ExecuteNonQuery();
        //        cn.Close();
        //        stockIn.LoadStockIn();

        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show(ex.Message, stitle);
        //    }
        //}
        private void dgvProduct_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string colName = dgvProduct.Columns[e.ColumnIndex].Name;
            if (colName == "Select")
            {
                if (string.IsNullOrWhiteSpace(stockIn.txtStockInBy.Text))
                {
                    MessageBox.Show("Please enter stock in by name first.", stitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    stockIn.txtStockInBy.Focus();
                    return;
                }

                if (MessageBox.Show("Add this item?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string pcode = dgvProduct.Rows[e.RowIndex].Cells[1].Value.ToString();
                    int qty = AskQuantity(); // prompt user
                    stockIn.ScanBarcodeManual(pcode, qty);
                    MessageBox.Show("Successfully added", stitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }


        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadProduct();
        }

        private void ProductStockIn_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Dispose();
            }
        }



        private void ProductStockIn_Load(object sender, EventArgs e)
        {

        }

        private void txtBarcode_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)13) // Enter key
            {
                string barcode = txtBarcode.Text.Trim();
                if (!string.IsNullOrEmpty(barcode))
                {
                    // Send the barcode to StockIn form
                    stockIn.ScanBarcode(barcode);

                    txtBarcode.Clear();
                }
                e.Handled = true; // prevent beep / double firing
            }
        }


        
    }
}