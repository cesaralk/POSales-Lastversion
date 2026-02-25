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
    public partial class Record : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;

        // Runtime columns
        private const string ColInvCostPrice = "colInvCostPrice";
        private const string ColInvMarkup = "colInvMarkup";
        private const string ColStockInCostPrice = "colStockInCostPrice";
        private const string ColStockInMarkup = "colStockInMarkup";
        private const string ColStockInPrice = "colStockInPrice";
        public Record()
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            LoadCriticalItems();
            LoadInventoryList();
        }

        private void EnsureInventoryPricingColumns()
        {
            if (!dgvInventoryList.Columns.Contains(ColInvCostPrice))
            {
                dgvInventoryList.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = ColInvCostPrice,
                    HeaderText = "Cost Price",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    ReadOnly = true,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0.00" }
                });
            }
            if (!dgvInventoryList.Columns.Contains(ColInvMarkup))
            {
                dgvInventoryList.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = ColInvMarkup,
                    HeaderText = "Markup %",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    ReadOnly = true,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0.##" }
                });
            }
        }

        private void EnsureStockInPricingColumns()
        {
            if (!dgvStockIn.Columns.Contains(ColStockInCostPrice))
            {
                dgvStockIn.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = ColStockInCostPrice,
                    HeaderText = "Cost Price",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    ReadOnly = true,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0.00" }
                });
            }
            if (!dgvStockIn.Columns.Contains(ColStockInMarkup))
            {
                dgvStockIn.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = ColStockInMarkup,
                    HeaderText = "Markup %",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    ReadOnly = true,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0.##" }
                });
            }
            if (!dgvStockIn.Columns.Contains(ColStockInPrice))
            {
                dgvStockIn.Columns.Add(new DataGridViewTextBoxColumn
                {
                    Name = ColStockInPrice,
                    HeaderText = "Price",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                    ReadOnly = true,
                    DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "#,##0.00" }
                });
            }
        }

        public void LoadTopSelling()
        {
            int i = 0;
            dgvTopSelling.Rows.Clear();
            cn.Open();

            //Sort By Total Amount
            if (cbTopSell.Text == "Sort By Qty")
            {
                cm = new SqlCommand("SELECT TOP 10 pcode, pdesc, isnull(sum(qty),0) AS qty, ISNULL(SUM(total),0) AS total FROM vwTopSelling WHERE sdate BETWEEN '" + dtFromTopSell.Value.ToString() + "' AND '" + dtToTopSell.Value.ToString() + "' AND status LIKE 'Sold' GROUP BY pcode, pdesc ORDER BY qty DESC", cn);
            }
            else if (cbTopSell.Text == "Sort By Total Amount")
            {
                cm = new SqlCommand("SELECT TOP 10 pcode, pdesc, isnull(sum(qty),0) AS qty, ISNULL(SUM(total),0) AS total FROM vwTopSelling WHERE sdate BETWEEN '" + dtFromTopSell.Value.ToString() + "' AND '" + dtToTopSell.Value.ToString() + "' AND status LIKE 'Sold' GROUP BY pcode, pdesc ORDER BY total DESC", cn);
            }
            dr = cm.ExecuteReader();
            while(dr.Read())
            {
                i++;
                dgvTopSelling.Rows.Add(i, dr["pcode"].ToString(), dr["pdesc"].ToString(), dr["qty"].ToString(), double.Parse(dr["total"].ToString()).ToString("#,##0.00"));
            }
            dr.Close();
            cn.Close();
        }

        public void LoadSoldItems()
        {
            try
            {
                dgvSoldItems.Rows.Clear();
                int i = 0;
                cn.Open();
                cm = new SqlCommand("SELECT c.pcode, p.pdesc, c.price, sum(c.qty) as qty, SUM(c.disc) AS disc, SUM(c.total) AS total FROM tbCart AS c INNER JOIN tbProduct AS p ON c.pcode=p.pcode WHERE status LIKE 'Sold' AND sdate BETWEEN '" + dtFromSoldItems.Value.ToString() + "' AND '" + dtToSoldItems.Value.ToString() + "' GROUP BY c.pcode, p.pdesc, c.price",cn);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    dgvSoldItems.Rows.Add(i, dr["pcode"].ToString(), dr["pdesc"].ToString(), double.Parse(dr["price"].ToString()).ToString("#,##0.00"), dr["qty"].ToString(), dr["disc"].ToString(), double.Parse(dr["total"].ToString()).ToString("#,##0.00"));
                }
                dr.Close();
                cn.Close();

                cn.Open();
                cm = new SqlCommand("SELECT ISNULL(SUM(total),0) FROM tbCart WHERE status LIKE 'Sold' AND sdate BETWEEN '" + dtFromSoldItems.Value.ToString() + "' AND '" + dtToSoldItems.Value.ToString() + "'", cn);
                lblTotal.Text = double.Parse(cm.ExecuteScalar().ToString()).ToString("#,##0.00");
                cn.Close();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        public void LoadCriticalItems()
        {
            try
            {
                dgvCriticalItems.Rows.Clear();
                int i = 0;
                cn.Open();
                cm = new SqlCommand("SELECT * FROM vwCriticalItems",cn);
                dr = cm.ExecuteReader();
                while(dr.Read())
                {
                    i++;
                    dgvCriticalItems.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString(), dr[7].ToString());

                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }

        public void LoadInventoryList()
        {
            try
            {
                EnsureInventoryPricingColumns();
                dgvInventoryList.Rows.Clear();
                int i = 0;
                cn.Open();
                // Add costprice/markup from product master
                cm = new SqlCommand(@"SELECT v.*, p.costprice, p.markup FROM vwInventoryList v INNER JOIN tbProduct p ON p.pcode = v.pcode ORDER BY p.pdesc", cn);
                dr = cm.ExecuteReader();
                while (dr.Read())
                {
                    i++;
                    // original 8 columns + cost + markup
                    string cost = dr[dr.FieldCount - 2] == DBNull.Value ? "0.00" : double.Parse(dr[dr.FieldCount - 2].ToString()).ToString("#,##0.00");
                    string markup = dr[dr.FieldCount - 1] == DBNull.Value ? "0" : double.Parse(dr[dr.FieldCount - 1].ToString()).ToString("#,##0.##");

                    dgvInventoryList.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(), dr[6].ToString(), dr[7].ToString(), cost, markup);

                }
                dr.Close();
                cn.Close();
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }

        }

        public void LoadCancelItems()
        {
            int i = 0;
            dgvCancel.Rows.Clear();
            cn.Open();
            cm = new SqlCommand("SELECT * FROM vwCancelItems WHERE sdate BETWEEN '" + dtFromCancel.Value.ToString() + "' AND '" + dtToCancel.Value.ToString() + "' ORDER BY sdate DESC", cn);
            dr = cm.ExecuteReader();
            while(dr.Read())
            {
                i++;
                dgvCancel.Rows.Add(i, dr[0].ToString(), dr[1].ToString(), dr[2].ToString(), dr[3].ToString(), dr[4].ToString(), dr[5].ToString(),  DateTime.Parse(dr[6].ToString()).ToShortDateString(), dr[7].ToString(), dr[8].ToString(), dr[9].ToString(), dr[10].ToString());
            }
            dr.Close();
            cn.Close();
        }

        public void LoadStockInHist()
        {
            int i = 0;
            dgvStockIn.Rows.Clear();
            EnsureStockInPricingColumns();
            cn.Open();
            cm = new SqlCommand(@"
                SELECT
                    s.id,
                    s.refno,
                    s.pcode,
                    p.pdesc,
                    s.qty,
                    s.sdate,
                    s.stockinby,
                    s.status,
                    ISNULL(sup.supplier,'') AS supplier,
                    ISNULL(s.CostPrice, p.costprice) AS CostPrice,
                    ISNULL(s.Markup, p.markup) AS Markup,
                    ISNULL(s.Price, p.price) AS Price
                FROM tbStockIn s
                INNER JOIN tbProduct p ON p.pcode = s.pcode
                LEFT JOIN tbSupplier sup ON sup.id = s.supplierid
                WHERE cast(s.sdate AS date) BETWEEN @from AND @to AND s.status LIKE 'Done'
                ORDER BY s.sdate DESC, s.id DESC", cn);
            cm.Parameters.AddWithValue("@from", dtFromStockIn.Value.Date);
            cm.Parameters.AddWithValue("@to", dtToStockIn.Value.Date);
            dr = cm.ExecuteReader();
            while (dr.Read())
            {
                i++;
                dgvStockIn.Rows.Add(
                    i,
                    dr["id"].ToString(),
                    dr["refno"].ToString(),
                    dr["pcode"].ToString(),
                    dr["pdesc"].ToString(),
                    dr["qty"].ToString(),
                    DateTime.Parse(dr["sdate"].ToString()).ToShortDateString(),
                    dr["stockinby"].ToString(),
                    dr["status"].ToString(),
                    dr["supplier"].ToString(),
                    Convert.ToDecimal(dr["CostPrice"]).ToString("#,##0.00"),
                    Convert.ToDecimal(dr["Markup"]).ToString("#,##0.##"),
                    Convert.ToDecimal(dr["Price"]).ToString("#,##0.00")
                );
            }
            dr.Close();
            cn.Close();
        }

        private void btnLoadTopSell_Click(object sender, EventArgs e)
        {
            if(cbTopSell.Text== "Select sort type")
            {
                MessageBox.Show("Please select sort type from the dropdown list.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cbTopSell.Focus();
                return;
            }
            LoadTopSelling();
        }

        private void btnLoadSoldItems_Click(object sender, EventArgs e)
        {
            LoadSoldItems();
        }

        private void btnPrintSoldItems_Click(object sender, EventArgs e)
        {
            POSReport report = new POSReport();
            string param = "From : " + dtFromSoldItems.Value.ToString() + " To : " + dtToSoldItems.Value.ToString();
            report.LoadSoldItems("SELECT c.pcode, p.pdesc, c.price, sum(c.qty) as qty, SUM(c.disc) AS disc, SUM(c.total) AS total FROM tbCart AS c INNER JOIN tbProduct AS p ON c.pcode=p.pcode WHERE status LIKE 'Sold' AND sdate BETWEEN '" + dtFromSoldItems.Value.ToString() + "' AND '" + dtToSoldItems.Value.ToString() + "' GROUP BY c.pcode, p.pdesc, c.price",param);
            report.ShowDialog();
        }

        private void btnLoadCancel_Click(object sender, EventArgs e)
        {
            LoadCancelItems();
        }

        private void btnLoadStockIn_Click(object sender, EventArgs e)
        {
            LoadStockInHist();
        }

        private void btnPrintTopSell_Click(object sender, EventArgs e)
        {
            POSReport report = new POSReport();
            string param = "From : " + dtFromTopSell.Value.ToString() + " To : " + dtToTopSell.Value.ToString();
            if (cbTopSell.Text == "Sort By Qty")
            {
                report.LoadTopSelling("SELECT TOP 10 pcode, pdesc, isnull(sum(qty),0) AS qty, ISNULL(SUM(total),0) AS total FROM vwTopSelling WHERE sdate BETWEEN '" + dtFromTopSell.Value.ToString() + "' AND '" + dtToTopSell.Value.ToString() + "' AND status LIKE 'Sold' GROUP BY pcode, pdesc ORDER BY qty DESC", param, "TOP SELLING ITEMS SORT BY QTY");
            }
            else if (cbTopSell.Text == "Sort By Total Amount")
            {
                report.LoadTopSelling("SELECT TOP 10 pcode, pdesc, isnull(sum(qty),0) AS qty, ISNULL(SUM(total),0) AS total FROM vwTopSelling WHERE sdate BETWEEN '" + dtFromTopSell.Value.ToString() + "' AND '" + dtToTopSell.Value.ToString() + "' AND status LIKE 'Sold' GROUP BY pcode, pdesc ORDER BY total DESC", param, "TOP SELLING ITEMS SORY BY TOTAL AMOUNT");
            }
            report.ShowDialog();
        }

        private void btnPrintInventoryList_Click(object sender, EventArgs e)
        {
            POSReport report = new POSReport();
            report.LoadInventory("SELECT * FROM vwInventoryList");
            report.ShowDialog();
        }

        private void btnPrintCancel_Click(object sender, EventArgs e)
        {
            POSReport report = new POSReport();
            string param = "From : " + dtFromCancel.Value.ToString() + " To : " + dtToCancel.Value.ToString();
            report.LoadCancelledOrder("SELECT * FROM vwCancelItems WHERE sdate BETWEEN '" + dtFromCancel.Value.ToString() + "' AND '" + dtToCancel.Value.ToString() + "'", param);
            report.ShowDialog();
        }

        private void btnPrintStockIn_Click(object sender, EventArgs e)
        {
            POSReport report = new POSReport();
            string param = "From : " + dtFromStockIn.Value.ToString() + " To : " + dtToStockIn.Value.ToString();
            report.LoadStockInHist("SELECT * FROM vwStockIn WHERE cast(sdate AS date) BETWEEN '" + dtFromStockIn.Value.ToString() + "' AND '" + dtToStockIn.Value.ToString() + "' AND status LIKE 'Done'", param);
            report.ShowDialog();
        }
    }
}
