/*using System;
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
    public partial class ProductModule : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        string stitle = "Point Of Sales";
        Product product;
        public ProductModule(Product pd)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            product = pd;
            LoadBrand();
            LoadCategory();
        }

        public void LoadCategory()
        {
            cboCategory.Items.Clear();
            cboCategory.DataSource = dbcon.getTable("SELECT * FROM tbCategory");
            cboCategory.DisplayMember = "category";
            cboCategory.ValueMember = "id";
        }

        public void LoadBrand()
        {
            cboBrand.Items.Clear();
            cboBrand.DataSource = dbcon.getTable("SELECT * FROM tbBrand");
            cboBrand.DisplayMember = "brand";
            cboBrand.ValueMember = "id";
        }

        private void picClose_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        public void Clear()
        {
            txtPcode.Clear();
            txtBarcode.Clear();
            txtPdesc.Clear();
            txtPrice.Clear();
            cboBrand.SelectedIndex = 0;
            cboCategory.SelectedIndex = 0;
            UDReOrder.Value = 1;

            txtPcode.Enabled = true;
            txtPcode.Focus();
            btnSave.Enabled = true;
            btnUpdate.Enabled = false;
        }
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure want to save this product?", "Save Product", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cm = new SqlCommand("INSERT INTO tbProduct(pcode, barcode, pdesc, bid, cid, price, reorder)VALUES (@pcode,@barcode,@pdesc,@bid,@cid,@price, @reorder)", cn);
                    cm.Parameters.AddWithValue("@pcode", txtPcode.Text);
                    cm.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                    cm.Parameters.AddWithValue("@pdesc", txtPdesc.Text);
                    cm.Parameters.AddWithValue("@bid", cboBrand.SelectedValue);
                    cm.Parameters.AddWithValue("@cid", cboCategory.SelectedValue);
                    cm.Parameters.AddWithValue("@price", double.Parse(txtPrice.Text));
                    cm.Parameters.AddWithValue("@reorder", UDReOrder.Value);
                    cn.Open();
                    cm.ExecuteNonQuery();
                    cn.Close();
                    MessageBox.Show("Product has been successfully saved.", stitle);
                    Clear();
                    product.LoadProduct();
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure want to update this product?", "Update Product", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    cm = new SqlCommand("UPDATE tbProduct SET barcode=@barcode,pdesc=@pdesc,bid=@bid,cid=@cid,price=@price, reorder=@reorder WHERE pcode LIKE @pcode", cn);
                    cm.Parameters.AddWithValue("@pcode", txtPcode.Text);
                    cm.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                    cm.Parameters.AddWithValue("@pdesc", txtPdesc.Text);
                    cm.Parameters.AddWithValue("@bid", cboBrand.SelectedValue);
                    cm.Parameters.AddWithValue("@cid", cboCategory.SelectedValue);
                    cm.Parameters.AddWithValue("@price", double.Parse(txtPrice.Text));
                    cm.Parameters.AddWithValue("@reorder", UDReOrder.Value);
                    cn.Open();
                    cm.ExecuteNonQuery();
                    cn.Close();
                    MessageBox.Show("Product has been successfully updated.", stitle);
                    Clear();
                    this.Dispose();
                }

            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        private void ProductModule_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Dispose();
            }
        }
    }
}
 */

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


/* namespace POSales
{
    public partial class ProductModule : Form
    {
        SqlConnection cn = new SqlConnection();
        DBConnect dbcon = new DBConnect();
        Product product;
        string stitle = "Point Of Sales";

        public ProductModule(Product pd)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            product = pd;

            LoadBrand();
            LoadCategory();
            Clear(); // Initialize form
        }

        // Load brands into combobox
        public void LoadBrand()
        {
            cboBrand.Items.Clear();
            cboBrand.DataSource = dbcon.getTable("SELECT * FROM tbBrand");
            cboBrand.DisplayMember = "brand";
            cboBrand.ValueMember = "id";
        }

        // Load categories into combobox
        public void LoadCategory()
        {
            cboCategory.Items.Clear();
            cboCategory.DataSource = dbcon.getTable("SELECT * FROM tbCategory");
            cboCategory.DisplayMember = "category";
            cboCategory.ValueMember = "id";
        }

        // Clear form and reset for new entry
        public void Clear()
        {
            txtPcode.Text = "AUTO";      // Show AUTO, disabled
            txtPcode.Enabled = false;

            txtBarcode.Clear();
            txtPdesc.Clear();
            txtPrice.Clear();
            cboBrand.SelectedIndex = 0;
            cboCategory.SelectedIndex = 0;
            UDReOrder.Value = 1;

            btnSave.Enabled = true;
            btnUpdate.Enabled = false;

            txtPdesc.Focus();
        }

        // Close form
        private void picClose_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        // Save new product
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to save this product?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string barcode = txtBarcode.Text.Trim();

                    // ❌ Check if barcode is empty
                    if (string.IsNullOrEmpty(barcode))
                    {
                        MessageBox.Show("Please enter a barcode.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    // 0️⃣ Check if barcode already exists
                    cn.Open();
                    using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(*) FROM tbProduct WHERE barcode=@barcode", cn))
                    {
                        cmdCheck.Parameters.AddWithValue("@barcode", barcode);
                        int count = (int)cmdCheck.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("This barcode is already assigned to another product.", "Duplicate Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            cn.Close();
                            return; // stop saving
                        }
                    }

                    // 1️⃣ Generate next unique pcode
                    int nextNum = 1;
                    using (SqlCommand cmdMax = new SqlCommand(
                        "SELECT MAX(CAST(SUBSTRING(pcode,3,6) AS INT)) FROM tbProduct WHERE pcode LIKE 'P-%'", cn))
                    {
                        object result = cmdMax.ExecuteScalar();
                        if (result != DBNull.Value)
                        {
                            nextNum = Convert.ToInt32(result) + 1;
                        }
                    }

                    string newPCode = "P-" + nextNum.ToString("D6");

                    // 2️⃣ Insert product
                    using (SqlCommand cm = new SqlCommand(
                        "INSERT INTO tbProduct(pcode, barcode, pdesc, bid, cid, price, reorder) " +
                        "VALUES (@pcode, @barcode, @pdesc, @bid, @cid, @price, @reorder)", cn))
                    {
                        cm.Parameters.AddWithValue("@pcode", newPCode);
                        cm.Parameters.AddWithValue("@barcode", barcode);
                        cm.Parameters.AddWithValue("@pdesc", txtPdesc.Text.Trim());
                        cm.Parameters.AddWithValue("@bid", cboBrand.SelectedValue);
                        cm.Parameters.AddWithValue("@cid", cboCategory.SelectedValue);
                        cm.Parameters.AddWithValue("@price", double.Parse(txtPrice.Text));
                        cm.Parameters.AddWithValue("@reorder", UDReOrder.Value);

                        cm.ExecuteNonQuery();
                    }
                    cn.Close();

                    MessageBox.Show("Product has been successfully saved.\nPCode: " + newPCode, stitle);
                    Clear();
                    product.LoadProduct();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Update existing product
        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to update this product?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string barcode = txtBarcode.Text.Trim();

                    // Check if barcode is assigned to another product (excluding current pcode)
                    cn.Open();
                    using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(*) FROM tbProduct WHERE barcode=@barcode AND pcode<>@pcode", cn))
                    {
                        cmdCheck.Parameters.AddWithValue("@barcode", barcode);
                        cmdCheck.Parameters.AddWithValue("@pcode", txtPcode.Text);
                        int count = (int)cmdCheck.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("This barcode is already assigned to another product.", "Duplicate Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            cn.Close();
                            return;
                        }
                    }

                    // Update product
                    using (SqlCommand cm = new SqlCommand(
                        "UPDATE tbProduct SET barcode=@barcode, pdesc=@pdesc, bid=@bid, cid=@cid, price=@price, reorder=@reorder WHERE pcode=@pcode", cn))
                    {
                        cm.Parameters.AddWithValue("@pcode", txtPcode.Text);
                        cm.Parameters.AddWithValue("@barcode", barcode);
                        cm.Parameters.AddWithValue("@pdesc", txtPdesc.Text.Trim());
                        cm.Parameters.AddWithValue("@bid", cboBrand.SelectedValue);
                        cm.Parameters.AddWithValue("@cid", cboCategory.SelectedValue);
                        cm.Parameters.AddWithValue("@price", double.Parse(txtPrice.Text));
                        cm.Parameters.AddWithValue("@reorder", UDReOrder.Value);

                        cm.ExecuteNonQuery();
                    }
                    cn.Close();

                    MessageBox.Show("Product has been successfully updated.", stitle);
                    Clear();
                    this.Dispose();
                    product.LoadProduct();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open)
                    cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Cancel button clears the form
        private void btnCancel_Click(object sender, EventArgs e)
        {
            Clear();
        }

        // Close form with Escape
        private void ProductModule_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Dispose();
        }
    }
}*/



namespace POSales
{
    public partial class ProductModule : Form
    {
        SqlConnection cn = new SqlConnection();
        DBConnect dbcon = new DBConnect();
        Product product;
        string stitle = "Point Of Sales";

        public ProductModule(Product pd)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            product = pd;

            LoadBrand();
            LoadCategory();
            Clear(); // Initialize form

            // Hook up Add Brand button (you need to add this button in designer)
            btnAddBrand.Click += btnAddBrand_Click;
            txtMarkup.TextChanged += (s, e) => CalculatePrice();

        }
        private void CalculatePrice()
        {
            double cost = 0;
            double markup = 0;

            double.TryParse(txtCostPrice.Text, out cost);
            double.TryParse(txtMarkup.Text, out markup);

            double selling = cost + (cost * markup / 100);

            txtPrice.Text = selling.ToString("0.00");
        }

        // Load brands into combobox
        public void LoadBrand()
        {
            DataTable dt = dbcon.getTable("SELECT * FROM tbBrand");
            cboBrand.DataSource = dt;
            cboBrand.DisplayMember = "brand";
            cboBrand.ValueMember = "id";
        }

        // Load categories into combobox
        public void LoadCategory()
        {
            cboCategory.Items.Clear();
            cboCategory.DataSource = dbcon.getTable("SELECT * FROM tbCategory");
            cboCategory.DisplayMember = "category";
            cboCategory.ValueMember = "id";
        }

        // Clear form and reset for new entry
        public void Clear()
        {
            txtPcode.Text = "AUTO";      // Show AUTO, disabled
            txtPcode.Enabled = false;

            txtBarcode.Clear();
            txtPdesc.Clear();
            txtCostPrice.Clear();
            txtPrice.Clear();
            cboBrand.SelectedIndex = 0;
            cboCategory.SelectedIndex = 0;
            UDReOrder.Value = 1;

            btnSave.Enabled = true;
            btnUpdate.Enabled = false;

            txtPdesc.Focus();
        }

        // Close form
        private void picClose_Click(object sender, EventArgs e)
        {
            this.Dispose();
        }

        // Helper prompt dialog
        private string Prompt(string text, string caption)
        {
            using (Form prompt = new Form())
            {
                prompt.Width = 300;
                prompt.Height = 150;
                prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
                prompt.Text = caption;
                prompt.StartPosition = FormStartPosition.CenterScreen;

                Label textLabel = new Label() { Left = 10, Top = 20, Text = text, AutoSize = true };
                TextBox inputBox = new TextBox() { Left = 10, Top = 50, Width = 260 };
                Button confirmation = new Button() { Text = "OK", Left = 180, Width = 80, Top = 80, DialogResult = DialogResult.OK };

                prompt.Controls.Add(textLabel);
                prompt.Controls.Add(inputBox);
                prompt.Controls.Add(confirmation);
                prompt.AcceptButton = confirmation;

                return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text.Trim() : "";
            }
        }

        // Add new brand directly
        private void btnAddBrand_Click(object sender, EventArgs e)
        {
            string brandName = Prompt("Enter new brand name:", "Add Brand");
            if (string.IsNullOrWhiteSpace(brandName)) return;

            try
            {
                cn.Open();

                // Check for duplicate
                using (SqlCommand cm = new SqlCommand("SELECT COUNT(*) FROM tbBrand WHERE brand=@brand", cn))
                {
                    cm.Parameters.AddWithValue("@brand", brandName);
                    int count = (int)cm.ExecuteScalar();
                    if (count > 0)
                    {
                        MessageBox.Show("This brand already exists.", "Duplicate Brand", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                // Insert new brand
                using (SqlCommand cm = new SqlCommand("INSERT INTO tbBrand(brand) VALUES(@brand)", cn))
                {
                    cm.Parameters.AddWithValue("@brand", brandName);
                    cm.ExecuteNonQuery();
                }

                cn.Close();
                LoadBrand();
                cboBrand.SelectedIndex = cboBrand.Items.Count - 1; // Select newly added brand
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Save new product
        //private void btnSave_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (MessageBox.Show("Are you sure you want to save this product?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        //        {
        //            string barcode = txtBarcode.Text.Trim();

        //            if (string.IsNullOrEmpty(barcode))
        //            {
        //                MessageBox.Show("Please enter a barcode.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                return;
        //            }

        //            cn.Open();

        //            // Check duplicate barcode
        //            using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(*) FROM tbProduct WHERE barcode=@barcode", cn))
        //            {
        //                cmdCheck.Parameters.AddWithValue("@barcode", barcode);
        //                int count = (int)cmdCheck.ExecuteScalar();
        //                if (count > 0)
        //                {
        //                    MessageBox.Show("This barcode is already assigned to another product.", "Duplicate Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                    cn.Close();
        //                    return;
        //                }
        //            }

        //            // Generate next PCode
        //            int nextNum = 1;
        //            using (SqlCommand cmdMax = new SqlCommand(
        //                "SELECT MAX(CAST(SUBSTRING(pcode,3,6) AS INT)) FROM tbProduct WHERE pcode LIKE 'P-%'", cn))
        //            {
        //                object result = cmdMax.ExecuteScalar();
        //                if (result != DBNull.Value) nextNum = Convert.ToInt32(result) + 1;
        //            }

        //            string newPCode = "P-" + nextNum.ToString("D6");

        //            // Insert product
        //            using (SqlCommand cm = new SqlCommand(
        //                "INSERT INTO tbProduct(pcode, barcode, pdesc, bid, cid, CostPrice, price, reorder) " +
        //                "VALUES(@pcode, @barcode, @pdesc, @bid, @cid, @costprice, @price, @reorder)", cn))
        //            {
        //                cm.Parameters.AddWithValue("@pcode", newPCode);
        //                cm.Parameters.AddWithValue("@barcode", barcode);
        //                cm.Parameters.AddWithValue("@pdesc", txtPdesc.Text.Trim());
        //                cm.Parameters.AddWithValue("@bid", cboBrand.SelectedValue);
        //                cm.Parameters.AddWithValue("@cid", cboCategory.SelectedValue);
        //                double cost = 0;
        //                if (!double.TryParse(txtCostPrice.Text, out cost))
        //                {
        //                    MessageBox.Show("Invalid cost price");
        //                    return;
        //                }

        //                cm.Parameters.AddWithValue("@costprice", cost);


        //                cm.Parameters.AddWithValue("@price", double.Parse(txtPrice.Text));
        //                cm.Parameters.AddWithValue("@reorder", UDReOrder.Value);

        //                cm.ExecuteNonQuery();
        //            }

        //            cn.Close();
        //            MessageBox.Show("Product has been successfully saved.\nPCode: " + newPCode, stitle);
        //            Clear();
        //            product.LoadProduct();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        if (cn.State == ConnectionState.Open) cn.Close();
        //        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}
        private void btnSave_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to save this product?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string barcode = txtBarcode.Text.Trim();
                    if (string.IsNullOrEmpty(barcode))
                    {
                        MessageBox.Show("Please enter a barcode.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    cn.Open();

                    // Check duplicate barcode
                    using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(*) FROM tbProduct WHERE barcode=@barcode", cn))
                    {
                        cmdCheck.Parameters.AddWithValue("@barcode", barcode);
                        int count = (int)cmdCheck.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("This barcode is already assigned to another product.", "Duplicate Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            cn.Close();
                            return;
                        }
                    }

                    // Generate next PCode
                    int nextNum = 1;
                    using (SqlCommand cmdMax = new SqlCommand(
                        "SELECT MAX(CAST(SUBSTRING(pcode,3,6) AS INT)) FROM tbProduct WHERE pcode LIKE 'P-%'", cn))
                    {
                        object result = cmdMax.ExecuteScalar();
                        if (result != DBNull.Value)
                            nextNum = Convert.ToInt32(result) + 1;
                    }

                    string newPCode = "P-" + nextNum.ToString("D6");

                    // Parse numbers
                    double cost = 0, markup = 0, price = 0;
                    double.TryParse(txtCostPrice.Text, out cost);
                    double.TryParse(txtMarkup.Text, out markup);
                    double.TryParse(txtPrice.Text, out price);

                    // Insert product
                    using (SqlCommand cm = new SqlCommand(
                        "INSERT INTO tbProduct(pcode, barcode, pdesc, bid, cid, CostPrice, price, Markup, reorder) " +
                        "VALUES(@pcode, @barcode, @pdesc, @bid, @cid, @costprice, @price, @markup, @reorder)", cn))
                    {
                        cm.Parameters.AddWithValue("@pcode", newPCode);
                        cm.Parameters.AddWithValue("@barcode", barcode);
                        cm.Parameters.AddWithValue("@pdesc", txtPdesc.Text.Trim());
                        cm.Parameters.AddWithValue("@bid", cboBrand.SelectedValue);
                        cm.Parameters.AddWithValue("@cid", cboCategory.SelectedValue);
                        cm.Parameters.AddWithValue("@costprice", cost);
                        cm.Parameters.AddWithValue("@price", price);
                        cm.Parameters.AddWithValue("@markup", markup);
                        cm.Parameters.AddWithValue("@reorder", UDReOrder.Value);

                        cm.ExecuteNonQuery();
                    }

                    cn.Close();
                    MessageBox.Show("Product has been successfully saved.\nPCode: " + newPCode, stitle);
                    Clear();
                    product.LoadProduct();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Update existing product
        //private void btnUpdate_Click(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        if (MessageBox.Show("Are you sure you want to update this product?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        //        {
        //            string barcode = txtBarcode.Text.Trim();

        //            cn.Open();
        //            // Check duplicate barcode excluding current product
        //            using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(*) FROM tbProduct WHERE barcode=@barcode AND pcode<>@pcode", cn))
        //            {
        //                cmdCheck.Parameters.AddWithValue("@barcode", barcode);
        //                cmdCheck.Parameters.AddWithValue("@pcode", txtPcode.Text);
        //                int count = (int)cmdCheck.ExecuteScalar();
        //                if (count > 0)
        //                {
        //                    MessageBox.Show("This barcode is already assigned to another product.", "Duplicate Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        //                    cn.Close();
        //                    return;
        //                }
        //            }

        //            // Update product
        //            using (SqlCommand cm = new SqlCommand(
        //                // "UPDATE tbProduct SET barcode=@barcode, pdesc=@pdesc, bid=@bid, cid=@cid, price=@price, reorder=@reorder WHERE pcode=@pcode", cn))
        //                "UPDATE tbProduct SET barcode=@barcode, pdesc=@pdesc, bid=@bid, cid=@cid, CostPrice=@cost, price=@price, reorder=@reorder WHERE pcode=@pcode", cn))

        //            {
        //                cm.Parameters.AddWithValue("@pcode", txtPcode.Text);
        //                cm.Parameters.AddWithValue("@barcode", barcode);
        //                cm.Parameters.AddWithValue("@pdesc", txtPdesc.Text.Trim());
        //                cm.Parameters.AddWithValue("@bid", cboBrand.SelectedValue);
        //                cm.Parameters.AddWithValue("@cid", cboCategory.SelectedValue);
        //                cm.Parameters.AddWithValue("@cost", double.Parse(txtCostPrice.Text));
        //                cm.Parameters.AddWithValue("@price", double.Parse(txtPrice.Text));
        //                cm.Parameters.AddWithValue("@reorder", UDReOrder.Value);

        //                cm.ExecuteNonQuery();
        //            }

        //            cn.Close();
        //            MessageBox.Show("Product has been successfully updated.", stitle);
        //            Clear();
        //            this.Dispose();
        //            product.LoadProduct();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        if (cn.State == ConnectionState.Open) cn.Close();
        //        MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        //    }
        //}

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            try
            {
                if (MessageBox.Show("Are you sure you want to update this product?", stitle, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    string barcode = txtBarcode.Text.Trim();

                    cn.Open();

                    // Check duplicate barcode excluding current product
                    using (SqlCommand cmdCheck = new SqlCommand("SELECT COUNT(*) FROM tbProduct WHERE barcode=@barcode AND pcode<>@pcode", cn))
                    {
                        cmdCheck.Parameters.AddWithValue("@barcode", barcode);
                        cmdCheck.Parameters.AddWithValue("@pcode", txtPcode.Text);
                        int count = (int)cmdCheck.ExecuteScalar();
                        if (count > 0)
                        {
                            MessageBox.Show("This barcode is already assigned to another product.", "Duplicate Barcode", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            cn.Close();
                            return;
                        }
                    }

                    // Parse numbers
                    double cost = 0, markup = 0, price = 0;
                    double.TryParse(txtCostPrice.Text, out cost);
                    double.TryParse(txtMarkup.Text, out markup);
                    double.TryParse(txtPrice.Text, out price);

                    // Update product
                    using (SqlCommand cm = new SqlCommand(
                        "UPDATE tbProduct SET barcode=@barcode, pdesc=@pdesc, bid=@bid, cid=@cid, CostPrice=@costprice, price=@price, Markup=@markup, reorder=@reorder WHERE pcode=@pcode", cn))
                    {
                        cm.Parameters.AddWithValue("@pcode", txtPcode.Text);
                        cm.Parameters.AddWithValue("@barcode", barcode);
                        cm.Parameters.AddWithValue("@pdesc", txtPdesc.Text.Trim());
                        cm.Parameters.AddWithValue("@bid", cboBrand.SelectedValue);
                        cm.Parameters.AddWithValue("@cid", cboCategory.SelectedValue);
                        cm.Parameters.AddWithValue("@costprice", cost);
                        cm.Parameters.AddWithValue("@price", price);
                        cm.Parameters.AddWithValue("@markup", markup);
                        cm.Parameters.AddWithValue("@reorder", UDReOrder.Value);

                        cm.ExecuteNonQuery();
                    }

                    cn.Close();
                    MessageBox.Show("Product has been successfully updated.", stitle);
                    Clear();
                    this.Dispose();
                    product.LoadProduct();
                }
            }
            catch (Exception ex)
            {
                if (cn.State == ConnectionState.Open) cn.Close();
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Clear();
        }

        private void ProductModule_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Dispose();
        }
    }
}
