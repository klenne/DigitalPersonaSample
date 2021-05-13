using DPFP;
using DPFP.Capture;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace biopersonid
{
    public partial class Form1 : Form
    {
        DigitalPersonaEnrollHandler handlerEnroll;
        DigitalPersonaVerifyHandler handlerVerify;
        DigitalPersonaDeleteHandler handlerDelete;

        public Form1()
        {
            InitializeComponent();
            handlerEnroll = new DigitalPersonaEnrollHandler();
            handlerEnroll.OnTemplate += Template;
            handlerVerify = new DigitalPersonaVerifyHandler();
            handlerVerify.OnVerify += Verifyed;
            handlerDelete = new DigitalPersonaDeleteHandler();
            handlerDelete.OnDelete += Deleted;
        }
        private void Template(DPFP.Template template)
        {
            try
            {
                var FileReader = new CargaFileHandler();
                var biometrias = new List<BiometriaFileJson>();
                biometrias.Add(new BiometriaFileJson(template.Bytes));
                FileReader.SavaBiometriasFile(biometrias, "digitalPersona", false);

                MessageBox.Show("Biometria cadastrada");

            }
            catch (Exception ex)
            {

            }
        }
        private void Verifyed(bool status, string chave)
        {
            try
            {
                MessageBox.Show($"{(status ? "" : "Não")} Verificou! \n {(status ? $"Chave: {chave}" : "")}");
                if (status)
                {
                    StopAll();
                }



            }
            catch (Exception ex)
            {

            }
        }

        private void Deleted(List<string> chaves)
        {
            MessageBox.Show("Deletado");
            StopAll();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                var readers = new DPFP.Capture.ReadersCollection();
 
            }
            catch {}

        }

        private void button1_Click(object sender, EventArgs e)
        {
            StopAll();
            handlerEnroll.Start();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            StopAll();
            handlerVerify.Start();

        }

        private void button3_Click(object sender, EventArgs e)
        {
            StopAll();
            handlerDelete.Start();
        }

        private void StopAll()
        {
            handlerEnroll.Stop();
            handlerDelete.Stop();
            handlerVerify.Stop();
        }

    }
}

