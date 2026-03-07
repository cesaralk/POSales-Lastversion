using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;

namespace POSales
{
    public partial class Recept : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();
        SqlDataReader dr;
        string store;
        string address;
        Cashier cashier;
        public Recept(Cashier cash)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            cashier = cash;
            LoadStore();
        }

        public void LoadStore()
        {
            cn.Open();
            cm = new SqlCommand("SELECT * FROM tbStore", cn);
            dr = cm.ExecuteReader();
            dr.Read();
            if (dr.HasRows)
            {
                store = dr["store"].ToString();
                address = dr["address"].ToString();
            }
            dr.Close();
            cn.Close();
        }

        private void Recept_Load(object sender, EventArgs e)
        {

            this.reportViewer1.RefreshReport();
        }

        private void SetReportParams(ReportViewer rv, string totalUSD, string cashUSD, string changeUSD, string totalLBP, string cashLBP, string changeLBP)
        {
            var reportParams = new List<ReportParameter> {
                new ReportParameter("pVatable", cashier.lblVatable.Text),
                new ReportParameter("pVat", cashier.lblVat.Text),
                new ReportParameter("pDiscount", cashier.lblDiscount.Text),
                new ReportParameter("pTotal", totalUSD),
                new ReportParameter("pCash", cashUSD),
                new ReportParameter("pChange", changeUSD),
                new ReportParameter("pTotalLBP", totalLBP),
                new ReportParameter("pCashLBP", cashLBP),
                new ReportParameter("pChangeLBP", changeLBP),
                new ReportParameter("pStore", store),
                new ReportParameter("pAddress", address),
                new ReportParameter("pTransaction", "Invoice #: " + cashier.lblTranNo.Text),
                new ReportParameter("pCashier", cashier.lblUsername.Text),
            };

            rv.LocalReport.SetParameters(reportParams);
        }

        public void LoadRecept(string totalUSD, string cashUSD, string changeUSD, string totalLBP, string cashLBP, string changeLBP)
        {
            ReportDataSource rptDataSourece;
            try
            {
                this.reportViewer1.LocalReport.ReportPath = Application.StartupPath + @"\Reports\rptRecept.rdlc";
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                SqlDataAdapter da = new SqlDataAdapter();

                cn.Open();
                da.SelectCommand = new SqlCommand("SELECT c.id, c.transno, c.pcode, c.price, c.qty, c.disc, c.total, c.sdate, c.status, p.pdesc FROM tbCart AS c INNER JOIN tbProduct AS p ON p.pcode=c.pcode WHERE c.transno LIKE '" + cashier.lblTranNo.Text + "'", cn);
                da.Fill(ds.Tables["dtRecept"]);
                cn.Close();

                this.SetReportParams(reportViewer1, totalUSD, cashUSD, changeUSD, totalLBP, cashLBP, changeLBP);

                rptDataSourece = new ReportDataSource("DataSet1", ds.Tables["dtRecept"]);
                reportViewer1.LocalReport.DataSources.Add(rptDataSourece);
                reportViewer1.SetDisplayMode(Microsoft.Reporting.WinForms.DisplayMode.PrintLayout);
                reportViewer1.ZoomMode = ZoomMode.Percent;
                reportViewer1.ZoomPercent = 30;
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message);
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="totalUSD"></param>
        /// <param name="cashUSD"></param>
        /// <param name="changeUSD"></param>
        /// <param name="totalLBP"></param>
        /// <param name="cashLBP"></param>
        /// <param name="changeLBP"></param>
        public void PrintReceptDirectly(string totalUSD, string cashUSD, string changeUSD, string totalLBP, string cashLBP, string changeLBP)
        {
            ReportDataSource rptDataSourece;
            try
            {
                this.reportViewer1.LocalReport.ReportPath = Application.StartupPath + @"\Reports\rptRecept.rdlc";
                this.reportViewer1.LocalReport.DataSources.Clear();

                DataSet1 ds = new DataSet1();
                SqlDataAdapter da = new SqlDataAdapter();

                cn.Open();
                da.SelectCommand = new SqlCommand("SELECT c.id, c.transno, c.pcode, c.price, c.qty, c.disc, c.total, c.sdate, c.status, p.pdesc FROM tbCart AS c INNER JOIN tbProduct AS p ON p.pcode=c.pcode WHERE c.transno LIKE '" + cashier.lblTranNo.Text + "'", cn);
                da.Fill(ds.Tables["dtRecept"]);
                cn.Close();

                this.SetReportParams(reportViewer1, totalUSD, cashUSD, changeUSD, totalLBP, cashLBP, changeLBP);

                rptDataSourece = new ReportDataSource("DataSet1", ds.Tables["dtRecept"]);
                reportViewer1.LocalReport.DataSources.Add(rptDataSourece);

                // Send report directly to printer using extension method
                reportViewer1.LocalReport.Print();
            }
            catch (Exception ex)
            {
                cn.Close();
                MessageBox.Show(ex.Message);
            }
        }

        private void Recept_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.Dispose();
            }
        }
    }
}
