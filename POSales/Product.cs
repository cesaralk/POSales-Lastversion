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

/*namespace POSales
{
    public partial class Product : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        public Product()
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            LoadProduct();
        }

        public void LoadProduct()
        {
            int i = 0;
            dgvProduct.Rows.Clear();
            cm = new SqlCommand("SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.price, p.reorder FROM tbProduct AS p INNER JOIN tbBrand AS b ON b.id = p.bid INNER JOIN tbCategory AS c on c.id = p.cid WHERE CONCAT(p.pdesc, b.brand, c.category) LIKE '%" +txtSearch.Text+ "%'",cn);
            cn.Open();
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                dgvProduct.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString());
            }
            dr.Close();
            cn.Close();
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            ProductModule productModule = new ProductModule(this);
            productModule.ShowDialog();
        }

        private void dgvProduct_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            string colName = dgvProduct.Columns[e.ColumnIndex].Name;
            if (colName == "Edit")
            {
                ProductModule product = new ProductModule(this);
                product.txtPcode.Text = dgvProduct.Rows[e.RowIndex].Cells[1].Value.ToString();
                product.txtBarcode.Text = dgvProduct.Rows[e.RowIndex].Cells[2].Value.ToString();
                product.txtPdesc.Text = dgvProduct.Rows[e.RowIndex].Cells[3].Value.ToString();
                product.cboBrand.Text = dgvProduct.Rows[e.RowIndex].Cells[4].Value.ToString();
                product.cboCategory.Text = dgvProduct.Rows[e.RowIndex].Cells[5].Value.ToString();
                product.txtPrice.Text = dgvProduct.Rows[e.RowIndex].Cells[6].Value.ToString();
                product.UDReOrder.Value = int.Parse(dgvProduct.Rows[e.RowIndex].Cells[7].Value.ToString());

                product.txtPcode.Enabled = false;
                product.btnSave.Enabled = false;
                product.btnUpdate.Enabled = true;
                product.ShowDialog();
            }
            else if (colName == "Delete")
            {
                if (MessageBox.Show("Are you sure you want to delete this record?", "Delete Record", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SqlCommand("DELETE FROM tbProduct WHERE pcode LIKE '" + dgvProduct[1, e.RowIndex].Value.ToString() + "'", cn);
                    cm.ExecuteNonQuery();
                    cn.Close();
                    MessageBox.Show("Product has been successfully deleted.", "Point Of Sales", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            LoadProduct();
        }

        private void txtSearch_TextChanged(object sender, EventArgs e)
        {
            LoadProduct();
        }
    }
}
*/


namespace POSales
{
    public partial class Product : Form
    {
        SqlConnection cn;
        SqlCommand cm;
        SqlDataReader dr;
        DBConnect dbcon = new DBConnect();

        // buffer for barcode scanner
        private string scanBuffer = "";

        public Product()
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());

            // VERY IMPORTANT: only ONE event
            txtSearch.KeyPress += txtSearch_KeyPress;

            LoadProduct();
        }

        // ================= LOAD PRODUCTS =================
        //public void LoadProduct()
        //{
        //    int i = 0;
        //    dgvProduct.Rows.Clear();

        //    string search = txtSearch.Text.Trim();

        //    string sql = @"
        //        SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.CostPrice, p.price, p.reorder
        //        FROM tbProduct p
        //        INNER JOIN tbBrand b ON p.bid = b.id
        //        INNER JOIN tbCategory c ON p.cid = c.id
        //        WHERE p.pdesc LIKE @search
        //           OR p.barcode LIKE @search
        //        ORDER BY p.pcode";

        //    cm = new SqlCommand(sql, cn);
        //    cm.Parameters.AddWithValue("@search", "%" + search + "%");

        //    cn.Open();
        //    dr = cm.ExecuteReader();
        //    while (dr.Read())
        //    {
        //        i++;
        //        dgvProduct.Rows.Add(
        //            i,
        //            dr["pcode"].ToString(),
        //            dr["barcode"].ToString(),
        //            dr["pdesc"].ToString(),
        //            dr["brand"].ToString(),
        //            dr["category"].ToString(),
        //            dr["costprice"].ToString(),
        //            dr["price"].ToString(),
        //            dr["reorder"].ToString()
        //        );
        //    }
        //    dr.Close();
        //    cn.Close();

        //    auto select first row
        //    if (dgvProduct.Rows.Count > 0)
        //    {
        //        dgvProduct.Rows[0].Selected = true;
        //    }
        //}
        public void LoadProduct()
        {
            int i = 0;
            dgvProduct.Rows.Clear();

            string search = txtSearch.Text.Trim();

            string sql = @"
        SELECT p.pcode, p.barcode, p.pdesc, b.brand, c.category, p.CostPrice, p.Markup, p.price, p.reorder
        FROM tbProduct p
        INNER JOIN tbBrand b ON p.bid = b.id
        INNER JOIN tbCategory c ON p.cid = c.id
        WHERE p.pdesc LIKE @search OR p.barcode LIKE @search
        ORDER BY p.pcode";

            cm = new SqlCommand(sql, cn);
            cm.Parameters.AddWithValue("@search", "%" + search + "%");

            cn.Open();
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                dgvProduct.Rows.Add(
                    i,
                    dr["pcode"].ToString(),
                    dr["barcode"].ToString(),
                    dr["pdesc"].ToString(),
                    dr["brand"].ToString(),
                    dr["category"].ToString(),
                    dr["costprice"].ToString(),
                    dr["markup"].ToString(),   // <-- Markup column
                    dr["price"].ToString(),
                    dr["reorder"].ToString()
                );
            }
            dr.Close();
            cn.Close();

            // Auto-select first row if exists
            if (dgvProduct.Rows.Count > 0)
            {
                dgvProduct.Rows[0].Selected = true;
            }
        }
        // ================= SEARCH (TYPE + SCAN) =================
        private void txtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            // scanner sends ENTER at the end
            if (e.KeyChar == (char)Keys.Enter)
            {
                txtSearch.Text = scanBuffer;
                LoadProduct();

                // reset for next scan
                scanBuffer = "";
                txtSearch.Clear();
                txtSearch.Focus();

                e.Handled = true;
                return;
            }

            // collect scanner characters
            if (!char.IsControl(e.KeyChar))
            {
                scanBuffer += e.KeyChar;
            }
        }

        // ================= ADD PRODUCT =================
        private void btnAdd_Click(object sender, EventArgs e)
        {
            ProductModule product = new ProductModule(this);
            product.ShowDialog();
        }

        // ================= EDIT / DELETE =================
        //private void dgvProduct_CellContentClick(object sender, DataGridViewCellEventArgs e)
        //{
        //    if (e.RowIndex < 0) return;

        //    string colName = dgvProduct.Columns[e.ColumnIndex].Name;
        //    string pcode = dgvProduct.Rows[e.RowIndex].Cells[1].Value.ToString();

        //    if (colName == "Edit")
        //    {
        //        ProductModule product = new ProductModule(this);

        //        product.txtPcode.Text = pcode;
        //        product.txtBarcode.Text = dgvProduct.Rows[e.RowIndex].Cells[2].Value.ToString();
        //        product.txtPdesc.Text = dgvProduct.Rows[e.RowIndex].Cells[3].Value.ToString();
        //        product.cboBrand.Text = dgvProduct.Rows[e.RowIndex].Cells[4].Value.ToString();
        //        product.cboCategory.Text = dgvProduct.Rows[e.RowIndex].Cells[5].Value.ToString();
        //        product.txtCostPrice.Text = dgvProduct.Rows[e.RowIndex].Cells[6].Value.ToString();
        //         product.txtPrice.Text = dgvProduct.Rows[e.RowIndex].Cells[7].Value.ToString();
        //        product.UDReOrder.Value = Convert.ToInt32(dgvProduct.Rows[e.RowIndex].Cells[8].Value);

        //        product.txtPcode.Enabled = false;
        //        product.btnSave.Enabled = false;
        //        product.btnUpdate.Enabled = true;

        //        product.ShowDialog();
        //    }
        //    else if (colName == "Delete")
        //    {
        //        if (MessageBox.Show(
        //            "Are you sure you want to delete this product?",
        //            "Delete Product",
        //            MessageBoxButtons.YesNo,
        //            MessageBoxIcon.Question) == DialogResult.Yes)
        //        {
        //            cn.Open();
        //            cm = new SqlCommand("DELETE FROM tbProduct WHERE pcode=@pcode", cn);
        //            cm.Parameters.AddWithValue("@pcode", pcode);
        //            cm.ExecuteNonQuery();
        //            cn.Close();

        //            MessageBox.Show("Product deleted successfully.", "POS");
        //            LoadProduct();
        //        }
        //    }
        private void dgvProduct_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colName = dgvProduct.Columns[e.ColumnIndex].Name;
            string pcode = dgvProduct.Rows[e.RowIndex].Cells[1].Value?.ToString() ?? "";

            if (colName == "Edit")
            {
                ProductModule product = new ProductModule(this);

                // Safe parsing from DataGridView cells
                string costText = dgvProduct.Rows[e.RowIndex].Cells[6].Value?.ToString() ?? "0";
                string markupText = dgvProduct.Rows[e.RowIndex].Cells[7].Value?.ToString() ?? "0";
                string priceText = dgvProduct.Rows[e.RowIndex].Cells[8].Value?.ToString() ?? "0";
                string reorderText = dgvProduct.Rows[e.RowIndex].Cells[9].Value?.ToString() ?? "1";

                product.txtPcode.Text = pcode;
                product.txtBarcode.Text = dgvProduct.Rows[e.RowIndex].Cells[2].Value?.ToString() ?? "";
                product.txtPdesc.Text = dgvProduct.Rows[e.RowIndex].Cells[3].Value?.ToString() ?? "";
                product.cboBrand.Text = dgvProduct.Rows[e.RowIndex].Cells[4].Value?.ToString() ?? "";
                product.cboCategory.Text = dgvProduct.Rows[e.RowIndex].Cells[5].Value?.ToString() ?? "";
                product.txtCostPrice.Text = costText;
                product.txtMarkup.Text = markupText;
                product.txtPrice.Text = priceText;

                int reorderValue = 1;
                int.TryParse(reorderText, out reorderValue);
                product.UDReOrder.Value = reorderValue;

                product.txtPcode.Enabled = false;
                product.btnSave.Enabled = false;
                product.btnUpdate.Enabled = true;

                product.ShowDialog();
            }
            else if (colName == "Delete")
            {
                if (MessageBox.Show(
                    "Are you sure you want to delete this product?",
                    "Delete Product",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cn.Open();
                    cm = new SqlCommand("DELETE FROM tbProduct WHERE pcode=@pcode", cn);
                    cm.Parameters.AddWithValue("@pcode", pcode);
                    cm.ExecuteNonQuery();
                    cn.Close();

                    MessageBox.Show("Product deleted successfully.", "POS");
                    LoadProduct();
                }
            }
        }
    }
    }
