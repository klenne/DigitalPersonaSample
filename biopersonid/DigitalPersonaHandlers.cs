using DPFP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace biopersonid
{
    //separei em classes diferentes para não dar error de memória
    //estou guardando as biometrias em um arquivo, pois implementei uma solução que sincroniza com uma api de biometrias, mas vocês podem gravar a base64 no banco ou mesmo os bytes... vai depender de como vai ser a solução
    public class DigitalPersonaDeleteHandler : CapturerHandler
    {

        public delegate void OnDeleteHandler(List<string> chaves);
        public event OnDeleteHandler OnDelete;
        public DigitalPersonaDeleteHandler()
        {
            this.Init();
        }


        protected override void Init()
        {
            base.Init();
            Verificator = new DPFP.Verification.Verification();

        }

        protected override void Process(DPFP.Sample Sample)
        {
            base.Process(Sample);


            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Verification);

            var FileReader = new CargaFileHandler();

            if (features != null)
            {

                var biometrias = FileReader.ReadBiometriasFile("digitalPersona");
                var chaves = new List<string>();
                foreach (var bio in biometrias)
                {
                    Template t = new Template();

                    t.DeSerialize(bio.GetBytes());

                    DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();
                    Verificator.Verify(features, t, ref result);
                    if (result.Verified)
                    {
                        chaves.Add(bio.Chave);
                    }
                }

                foreach (var chave in chaves)
                {
                    biometrias.RemoveAll(x => x.Chave == chave);
                }
                if (biometrias.Count > 0)
                    FileReader.SavaBiometriasFile(biometrias, "digitalPersona", true);
                OnDelete?.Invoke(chaves);
            }
        }




        private DPFP.Verification.Verification Verificator;


    }
    public class DigitalPersonaVerifyHandler : CapturerHandler
    {
        public delegate void OnVerifyEventHandler(bool status, string chave);
        public event OnVerifyEventHandler OnVerify;
        public DigitalPersonaVerifyHandler()
        {
            this.Init();
        }


        protected override void Init()
        {
            base.Init();
            Verificator = new DPFP.Verification.Verification();

        }

        protected override void Process(DPFP.Sample Sample)
        {
            base.Process(Sample);


            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Verification);


            var FileReader = new CargaFileHandler();

            if (features != null)
            {



                var biometrias = FileReader.ReadBiometriasFile("digitalPersona");


                foreach (var bio in biometrias)
                {
                    DPFP.Verification.Verification.Result result = new DPFP.Verification.Verification.Result();
                    try
                    {
                        Template t = new Template();
                        t.DeSerialize(bio.GetBytes());

                        Verificator.Verify(features, t, ref result);
                    }
                    catch { OnVerify?.Invoke(false, ""); return; };
                    if (result.Verified)
                    {
                        OnVerify?.Invoke(true, bio.Chave);
                        return;
                    }
                }
                OnVerify?.Invoke(false, "");
            }
        }





        private DPFP.Verification.Verification Verificator;

    }
    public class DigitalPersonaEnrollHandler : CapturerHandler
    {
        private DPFP.Processing.Enrollment Enroller;
        public delegate void OnTemplateEventHandler(DPFP.Template template);
        public event OnTemplateEventHandler OnTemplate;

        public DigitalPersonaEnrollHandler()
        {
            this.Init();
        }


        protected override void Init()
        {
            base.Init();

            Enroller = new DPFP.Processing.Enrollment();

        }



        protected override void Process(DPFP.Sample Sample)
        {

            base.Process(Sample);


            DPFP.FeatureSet features = ExtractFeatures(Sample, DPFP.Processing.DataPurpose.Enrollment);

            if (features != null) try
                {

                    Enroller.AddFeatures(features);
                }
                finally
                {



                    switch (Enroller.TemplateStatus)
                    {
                        case DPFP.Processing.Enrollment.Status.Ready:
                            OnTemplate(Enroller.Template);

                            Stop();
                            break;

                        case DPFP.Processing.Enrollment.Status.Failed:
                            Enroller.Clear();
                            Stop();

                            OnTemplate(null);
                            Start();
                            break;
                    }
                }


        }






    }
    public class CapturerHandler : DPFP.Capture.EventHandler
    {


        protected virtual void Init()
        {
            try
            {
                Capturer = new DPFP.Capture.Capture();
                if (null != Capturer)
                    Capturer.EventHandler = this;


            }
            catch
            {

            }
        }

        protected virtual void Process(DPFP.Sample Sample)
        {

            DrawPicture(ConvertSampleToBitmap(Sample));
        }

        public void Start()
        {
            if (null != Capturer)
            {
                try
                {
                    Capturer.StartCapture();

                }
                catch (Exception ex)
                {

                }
            }
        }

        public void Stop()
        {
            if (null != Capturer)
            {
                try
                {
                    Capturer.StopCapture();
                }
                catch
                {

                }
            }
        }



        #region EventHandler Members:

        public void OnComplete(object Capture, string ReaderSerialNumber, DPFP.Sample Sample)
        {

            Process(Sample);
        }

        public void OnFingerGone(object Capture, string ReaderSerialNumber)
        {

        }

        public void OnFingerTouch(object Capture, string ReaderSerialNumber)
        {
        }

        public void OnReaderConnect(object Capture, string ReaderSerialNumber)
        {
        }

        public void OnReaderDisconnect(object Capture, string ReaderSerialNumber)
        {
        }

        public void OnSampleQuality(object Capture, string ReaderSerialNumber, DPFP.Capture.CaptureFeedback CaptureFeedback)
        {

        }
        #endregion

        protected Bitmap ConvertSampleToBitmap(DPFP.Sample Sample)
        {
            DPFP.Capture.SampleConversion Convertor = new DPFP.Capture.SampleConversion();
            Bitmap bitmap = null;
            Convertor.ConvertToPicture(Sample, ref bitmap);
            return bitmap;
        }

        protected DPFP.FeatureSet ExtractFeatures(DPFP.Sample Sample, DPFP.Processing.DataPurpose Purpose)
        {
            DPFP.Processing.FeatureExtraction Extractor = new DPFP.Processing.FeatureExtraction();
            DPFP.Capture.CaptureFeedback feedback = DPFP.Capture.CaptureFeedback.None;
            DPFP.FeatureSet features = new DPFP.FeatureSet();
            Extractor.CreateFeatureSet(Sample, Purpose, ref feedback, ref features);
            if (feedback == DPFP.Capture.CaptureFeedback.Good)
                return features;
            else
                return null;
        }

        private void DrawPicture(Bitmap bitmap)
        {

        }

        private DPFP.Capture.Capture Capturer;

    }



    public class CargaFileHandler
    {
        public string path = Environment.CurrentDirectory;

        public List<BiometriaFileJson> ReadBiometriasFile(string fileName)
        {
            try
            {
                var biometrias = new List<BiometriaFileJson>();
                string raw;
                if (File.Exists(Path.Combine(path, $"{fileName}.txt")))
                {
                    raw = File.ReadAllText(Path.Combine(path, $"{fileName}.txt"));
                    try
                    {
                        biometrias = JsonConvert.DeserializeObject<List<BiometriaFileJson>>(raw);

                    }
                    catch { }
                }



                return biometrias;
            }
            catch { throw; }

        }

        public bool SavaBiometriasFile(List<BiometriaFileJson> biometrias, string fileName, bool isCargaCompleta)
        {
            if (!File.Exists(Path.Combine(path, $"{fileName}.txt")))
            {
                File.Create(Path.Combine(path, $"{fileName}.txt")).Close();
            }
            var biometriaList = new List<BiometriaFileJson>();

            if (isCargaCompleta)
            {
                try
                {
                    File.WriteAllText(Path.Combine(path, $"{fileName}.txt"), JsonConvert.SerializeObject(biometrias));
                }
                catch
                {
                    return false;
                }

            }
            else
            {

                try
                {
                    var json = File.ReadAllText(Path.Combine(path, $"{fileName}.txt"));
                    try { biometriaList = JsonConvert.DeserializeObject<List<BiometriaFileJson>>(json); } catch { }

                    if (biometriaList == null)
                    {
                        biometriaList = new List<BiometriaFileJson>();
                    }
                    var merged = biometriaList.Union(biometrias).ToList();
                    File.WriteAllText(Path.Combine(path, $"{fileName}.txt"), JsonConvert.SerializeObject(merged));

                }
                catch (Exception ex)
                {
                    return false;
                }


            }


            return true;
        }


    }
    public class BiometriaFileJson

    {
        public BiometriaFileJson() { }
        public BiometriaFileJson(byte[] bytes)
        {
            try { this.Chave = Convert.ToBase64String(bytes); }
            catch
            {
                this.Chave = "";
            }
        }
        public string Chave { get; set; }

        public byte[] GetBytes()
        {
            return Convert.FromBase64String(this.Chave);
        }
    }
}
