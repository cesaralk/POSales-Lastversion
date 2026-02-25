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
    public partial class Settle : Form
    {
        SqlConnection cn = new SqlConnection();
        SqlCommand cm = new SqlCommand();
        DBConnect dbcon = new DBConnect();        
        Cashier cashier;
        double exchangeRate = 89500;
        double totalUSD = 0;
        public Settle(Cashier cash)
        {
            InitializeComponent();
            cn = new SqlConnection(dbcon.myConnection());
            this.KeyPreview = true;
            cashier = cash;

            totalUSD = cashier.CurrentSaleUSD;

            // DISPLAY ONLY
            txtSale.Text = "$ " + totalUSD.ToString("#,##0.00");
            txtSaleLBP.Text = "L.L " + (totalUSD * exchangeRate).ToString("#,##0");

            // IMPORTANT: store numeric sale in TAG (hidden, safe)
            txtSale.Tag = totalUSD;

           

            // display USD and LBP
            txtSale.Text = "$ " + totalUSD.ToString("#,##0.00");
            txtSaleLBP.Text = "L.L " + (totalUSD * exchangeRate).ToString("#,##0");

            // store numeric value safely
            txtSale.Tag = totalUSD;

            // initialize change display
            txtChange.Text = "$ 0.00";
            txtChangeLBP.Text = "L.L 0";
        

        }



        private void btnOne_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnOne.Text;
        }

        private void btnTwo_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnTwo.Text;
        }

        private void btnThree_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnThree.Text;
        }

        private void btnFour_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnFour.Text;
        }

        private void btnFive_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnFive.Text;
        }

        private void btnSix_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnSix.Text;
        }

        private void btnSeven_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnSeven.Text;
        }

        private void btnEight_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnEight.Text;
        }

        private void btnNine_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnNine.Text;
        }

        private void btnZero_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnZero.Text;
        }

        private void btnDZero_Click(object sender, EventArgs e)
        {
            txtCash.Text += btnDZero.Text;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtCash.Clear();
            txtCash.Focus();
        }

        private void btnEnter_Click(object sender, EventArgs e)
        {
            try
            {
                //if ((double.Parse(txtChange.Text) < 0) || (txtCash.Text.Equals("")))
                //{
                //    MessageBox.Show("Insufficient amount, Please enter the corret amount!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                //    return;
                //}
                double changeUSD = (double.Parse(txtCash.Text == "" ? "0" : txtCash.Text)
                   + double.Parse(textCashLBP.Text == "" ? "0" : textCashLBP.Text) / exchangeRate)
                   - totalUSD;

                if (changeUSD < 0)
                {
                    MessageBox.Show("Insufficient amount, Please enter the correct amount!",
                        "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                else
                {
                    for(int i=0; i< cashier.dgvCash.Rows.Count; i++ )
                    {
                        cn.Open();
                        cm = new SqlCommand("UPDATE tbProduct SET qty = qty - " + int.Parse(cashier.dgvCash.Rows[i].Cells[5].Value.ToString()) + "WHERE pcode= '" + cashier.dgvCash.Rows[i].Cells[2].Value.ToString() + "'", cn);
                        cm.ExecuteNonQuery();
                        cn.Close();

                        cn.Open();
                        cm = new SqlCommand("UPDATE tbCart SET status = 'Sold' WHERE id= '" + cashier.dgvCash.Rows[i].Cells[1].Value.ToString() + "'", cn);
                        cm.ExecuteNonQuery();
                        cn.Close();
                    }
                    Recept recept = new Recept(cashier);

                    // Shows Dialog before print
                    // txtSale is amount due in $
                    // txtCash is amount paid in $
                    // txtChange is change in $
                    // txtSaleLBP is amount due in LBP
                    // textCashLBP is amount paid in LBP
                    // txtChangeLBP is change in LBP
                    //recept.LoadRecept(txtSale.Text, txtCash.Text, txtChange.Text, txtSaleLBP.Text, textCashLBP.Text, txtChangeLBP.Text);
                    //recept.ShowDialog();

                    // Print directly without showing dialog
                    recept.PrintReceptDirectly(txtSale.Text, txtCash.Text, txtChange.Text, txtSaleLBP.Text, textCashLBP.Text, txtChangeLBP.Text);

                    //MessageBox.Show("Payment successfully saved!", "Payment", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    cashier.GetTranNo();
                    cashier.LoadCart();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        //private void txtCash_TextChanged(object sender, EventArgs e)
        //{
        //    try
        //    {
        //        double sale = double.Parse(txtSale.Text);
        //        double cash = double.Parse(txtCash.Text);
        //        double charge = cash - sale;
        //        txtChange.Text = charge.ToString("#,##0.00");
        //    }
        //    catch (Exception)
        //    {
        //        txtChange.Text = "0.00";
        //    }
        //}
        private void txtCash_TextChanged(object sender, EventArgs e)
        {
            try
            {
                double saleUSD = (double)txtSale.Tag;

                double cashUSD = 0;
                double cashLBP = 0;

                if (!string.IsNullOrEmpty(txtCash.Text))
                    cashUSD = double.Parse(txtCash.Text);

                if (!string.IsNullOrEmpty(textCashLBP.Text))
                    cashLBP = double.Parse(textCashLBP.Text) / exchangeRate;

                double totalCashUSD = cashUSD + cashLBP;
                double changeUSD = totalCashUSD - saleUSD;

                txtChange.Text = "$ " + changeUSD.ToString("#,##0.00");
                txtChangeLBP.Text = "L.L " + (changeUSD * exchangeRate).ToString("#,##0");
            }
            catch
            {
                txtChange.Text = "$ 0.00";
                txtChangeLBP.Text = "L.L 0";
            }
        }


        private void Settle_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) this.Dispose();
            else if (e.KeyCode == Keys.Enter) btnEnter.PerformClick();            
        }
    }
}
