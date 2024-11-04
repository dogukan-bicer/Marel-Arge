using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.ML;
using Microsoft.ML.Data;
using static marel_arge.MainWindow;

namespace marel_arge
{
    public partial class MainWindow : Window
    {

        bool machine_learn_active = false;
        bool ml_train_ismotiondetected = false;
        int emg_ml_count,train_model_count = 0;
        ITransformer trainedModel = null;
        const int model_sample_size = ornek_sayisi;
        const int number_of_models= emg_rec_sample;

        const int ml_high_threshold_emg = 400;
        const int ml_low_threshold_emg = 10;

        MLContext mlContext = new MLContext();

        // Veri sınıfı
        public class EmgData
        {
            [ColumnName("Feature1_vec")]
            public float Feature1_vector { get; set; } // Tekil float olarak tanımlandı

            [ColumnName("Feature2_vec")]
            public float Feature2_vector { get; set; } // Tekil float olarak tanımlandı

            [ColumnName("motion_detect")]
            public bool Label { get; set; } // Hareket olup olmadığı (örneğin true veya false)

            [ColumnName("motion_detect2")]
            public bool Label2 { get; set; } // Hareket olup olmadığı (örneğin true veya false)
        }

        public class EmgPrediction
        {
            [ColumnName("PredictedLabel")]
            public bool PredictedLabel { get; set; }
            public float Probability { get; set; }
            public float Score { get; set; }
        }

        List<EmgData> trainingDataList = new List<EmgData>();
        List<float> emg_ml_train = new List<float>();
        List<float> emg2_ml_train = new List<float>();

        public void ml_train_start_ml(bool motion_detect, float emg_ml_train,float emg2_ml_train)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                emg_record_count_label.Content = train_model_count.ToString();
            }));
            // Eğitim verilerini hazırlama


                if (number_of_models > train_model_count)
                {
                    trainingDataList.Add(new EmgData
                    {
                        Feature1_vector = emg_ml_train,
                        Feature2_vector = emg2_ml_train,
                        Label = motion_detect
                    });
                    train_model_count++;
                }
                else
                {
                    ML_TrainModel_svm(trainingDataList);
                    emg_ml_count = 0;
                    train_model_count = 0;
                    machine_learn_active = false;
                }
            

            /// okuma fonksiyonunun içine at
        }
        public void ml_train_start(bool motion_detect,bool motion_detect2)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                emg_record_count_label.Content = train_model_count.ToString();
            }));
            // Eğitim verilerini hazırlama

            emg_ml_train.Add(emg_data);
            emg2_ml_train.Add(emg_data2);

            /// okuma fonksiyonunun içine at
            if (emg_ml_train.Count > model_sample_size) emg_ml_train.RemoveAt(0);
            if (emg2_ml_train.Count > model_sample_size) emg2_ml_train.RemoveAt(0);
            if (emg_ml_train.Count == model_sample_size && emg2_ml_train.Count == model_sample_size)
            {
                if(number_of_models > train_model_count)
                {
                    trainingDataList.Add(new EmgData
                    {
                        Feature1_vector = CalculateStandardDeviation_ml(emg_ml_train),
                        Feature2_vector = CalculateStandardDeviation_ml(emg2_ml_train),
                        Label = motion_detect,
                        Label2 = motion_detect2
                    });
                    train_model_count++;
                }
                else
                {
                    ML_TrainModel(trainingDataList);
                    emg_ml_count = 0;
                    train_model_count = 0;
                    machine_learn_active = false;
                }
            }

            /// okuma fonksiyonunun içine at
        }

        //ldsvm
        //linearsvm


        //LbfgsLogisticRegression
        //SdcaLogisticRegression
        //SgdCalibrated
        //FieldAwareFactorizationMachine

        public void ML_TrainModel(List<EmgData> trainingDataList)
        {
            // Pozitif ve negatif sınıfların sayısını kontrol et
            int positiveClassCount = trainingDataList.Count(x => x.Label == true);
            int negativeClassCount = trainingDataList.Count(x => x.Label == false);

            // Pozitif ve negatif sınıf sayısını kontrol et
            MessageBox.Show($"Pozitif örnek sayısı: {positiveClassCount}, Negatif örnek sayısı: {negativeClassCount}");

            // Pozitif sınıf yoksa eğitim yapma
            if (positiveClassCount == 0)
            {
                MessageBox.Show("Veride hiç pozitif sınıf örneği yok. AUC hesaplanamaz.");
                return;
            }

            // IDataView veri formatına dönüştür
            IDataView trainingData = mlContext.Data.LoadFromEnumerable(trainingDataList);

            // Eğitim ve test verilerini ayır (örneğin %80 eğitim, %20 test)
            var splitData = mlContext.Data.TrainTestSplit(trainingData, testFraction: 0.2);
            var trainData = splitData.TrainSet;
            var testData = splitData.TestSet;

            // Eğitim pipeline'ı oluştur ve veriyi normalleştir
            var pipeline = mlContext.Transforms.Concatenate("Features", "Feature1_vec", "Feature2_vec")
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))
                .Append(mlContext.BinaryClassification.Trainers.LbfgsLogisticRegression(
                    labelColumnName: "motion_detect",
                    featureColumnName: "Features",
                    optimizationTolerance: 1e-7f,  // Daha düşük tolerans
                    l2Regularization: 0.001f  // L2 Regularization hafifçe artırıldı
                ));

            // Modeli eğit
            var model = pipeline.Fit(trainingData);

            // Modeli kaydet
            mlContext.Model.Save(model, trainingData.Schema, "emg_model.zip");

            // Modelin doğruluğunu değerlendirme (evaluation)
            var predictions = model.Transform(testData);
            var metrics = mlContext.BinaryClassification.Evaluate(predictions, labelColumnName: "motion_detect");

            // Doğruluk sonuçlarını yazdır
            MessageBox.Show($"Accuracy: {metrics.Accuracy:P2}\nAUC: {metrics.AreaUnderRocCurve:P2}\nF1 Score: {metrics.F1Score:P2}");

            try
            {
                // Modeli yükle
                trainedModel = mlContext.Model.Load("emg_model.zip", out var modelInputSchema);
                ///
            }
            catch
            {
                //MessageBox.Show("Makine modeli açılamadı");
            }
        }

        public void ML_TrainModel_svm(List<EmgData> trainingDataList)
        {
            //trainingDataList = ApplySMOTE(trainingDataList); // SMOTE ile veri dengelenir
            //FeatureEngineering(trainingDataList); // Özellik mühendisliği uygulanır


            // Pozitif ve negatif sınıfların sayısını kontrol et
            int positiveClassCount = trainingDataList.Count(x => x.Label == true);
            int negativeClassCount = trainingDataList.Count(x => x.Label == false);

            // Pozitif ve negatif sınıf sayısını kontrol et
            MessageBox.Show($"Pozitif örnek sayısı: {positiveClassCount}, Negatif örnek sayısı: {negativeClassCount}");

            // Pozitif sınıf yoksa eğitim yapma
            if (positiveClassCount == 0)
            {
                MessageBox.Show("Veride hiç pozitif sınıf örneği yok. AUC hesaplanamaz.");
                return;
            }

            // MLContext oluştur
            MLContext mlContext = new MLContext();

            // IDataView veri formatına dönüştür
            IDataView trainingData = mlContext.Data.LoadFromEnumerable(trainingDataList);

            // Eğitim ve test verilerini ayır (örneğin %80 eğitim, %20 test)
            var splitData = mlContext.Data.TrainTestSplit(trainingData, testFraction: 0.2);
            var trainData = splitData.TrainSet;
            var testData = splitData.TestSet;

            // Eğitim pipeline'ı oluştur ve veriyi normalleştir
            // Eğitim pipeline'ı oluştur ve veriyi normalleştir
            var pipeline = mlContext.Transforms.Concatenate("Features", "Feature1_vec", "Feature2_vec")
                .Append(mlContext.Transforms.NormalizeMinMax("Features"))  // MinMax normalizasyonu
                .Append(mlContext.BinaryClassification.Trainers.LdSvm(  // LdSvm eğitici
                    labelColumnName: "motion_detect",
                    featureColumnName: "Features",
                    numberOfIterations: 100000  // Maksimum iterasyon sayısı
                ));
            // Modeli eğit
            var model = pipeline.Fit(trainData);

            // Modeli kaydet
            mlContext.Model.Save(model, trainData.Schema, "emg_model.zip");

            // Modelin doğruluğunu değerlendirme
            var predictions = model.Transform(testData);

            // Olasılık tahmini yerine SVM için Score (puan) kullanılır
            var metrics = mlContext.BinaryClassification.EvaluateNonCalibrated(predictions, labelColumnName: "motion_detect");

            // Doğruluk sonuçlarını yazdır
            MessageBox.Show($"Accuracy: {metrics.Accuracy:P2}\nPositive Precision: {metrics.PositivePrecision:P2}\nNegative Precision: {metrics.NegativePrecision:P2}");

            try
            {
                // Modeli yükle
                trainedModel = mlContext.Model.Load("emg_model.zip", out var modelInputSchema);
                ///
            }
            catch
            {
                //MessageBox.Show("Makine modeli açılamadı");
            }
        }

        public void ML_Predict(float emg_data, float emg_data2)
        {
            var predictor = mlContext.Model.CreatePredictionEngine<EmgData, EmgPrediction>(trainedModel);
            // Tahmin yap
            var prediction = predictor.Predict(new EmgData { Feature1_vector = emg_data, Feature2_vector = emg_data2 });
            emg_detect_1 = prediction.PredictedLabel;

            Dispatcher.BeginInvoke(new Action(() =>
            {
                test_label.Content = $"Tahmin edilen hareket: {emg_detect_1}, Olasılık: {prediction.Probability}";
            }));


            /////
            //////
            ///////
            if ((last_emg_data < ml_high_threshold_emg) & (last_emg_data > ml_low_threshold_emg))
            {
                if ((emg_detect_1) && (emg_detect_1 != last_motion) && (!emg_motion) && emg_enable)
                {
                    dataStr_1 = "0_0_0_0_0";//eller acık
                    Bluetooth_SendData(dataStr_1);
                    //timer_emg_motion.Tick += Timer_Tick_emg; // Timer'ın tetikleyicisini ayarla
                    emg_motion = true;
                    timer_emg_motion.Start();
                    last_motion = emg_detect_1;
                }
                else if ((emg_detect_1 != last_motion) && (!emg_motion) && emg_enable)
                {
                    dataStr_1 = "255_255_255_255_255";//eller kapalı
                    Bluetooth_SendData(dataStr_1);
                    emg_motion = true;
                    timer_emg_motion.Start();
                    last_motion = emg_detect_1;
                }
            }
            ///////
            /////
            ///
        }

        // Dosyadan verileri çekme ve makine öğrenmesi işlemini başlatma
        void ML_LoadData(string filePath)
        {
            //Cursor = Cursors.Wait; // Uncomment if you are using a UI framework and want to show a loading cursor.
            train_model_count = model_sample_size; // Veri hizalamak için

            // Dosyayı okuyup verileri işliyoruz
            try
            {
                foreach (var line in File.ReadLines(filePath))
                {
                    var values = line.Split('_');

                    // İlk sütundaki değeri '.' ile değiştirme ve float olarak alma
                    var emgData = float.Parse(values[0].Replace(',', '.'), CultureInfo.InvariantCulture);
                    var emgData2 = float.Parse(values[1].Replace(',', '.'), CultureInfo.InvariantCulture);

                    // 3. sütunu boolean'a çevirme
                    var motionDetected = bool.Parse(values[2]);

                    // Makine öğrenmesi eğitimine ekleyin
                    ml_train_start_ml(motionDetected, emgData, emgData2);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kayıt okunurken hata oluştu: " + ex.Message);
            }

            //Cursor = null; // Uncomment if you are using a UI framework and want to reset the cursor.
        }

        async public Task testml()
        {
            Random random = new Random();

            while (true)
            {
                emg_data = random.Next(1, 1000);
                emg_data2 = random.Next(1, 1000);
                await Task.Delay(10);
                UpdateEmgUI(emg_data.ToString(), emg_data.ToString());

                await emg_calculate_async();

                if (machine_learn_active)
                {
                    //ml_train_start(ml_train_ismotiondetected);
                    ml_train_start(emg_enable,emg_enable);
                }
            }
        }

        // SMOTE ile veri dengesini sağlayalım
        public List<EmgData> ApplySMOTE(List<EmgData> data)
        {
            // Simple oversampling (SMOTE gibi bir teknik kullanılabilir)
            var positiveSamples = data.Where(x => x.Label == true).ToList();
            var negativeSamples = data.Where(x => x.Label == false).ToList();

            // Eksik olan sınıfı tamamlayın
            if (positiveSamples.Count < negativeSamples.Count)
            {
                int difference = negativeSamples.Count - positiveSamples.Count;
                for (int i = 0; i < difference; i++)
                {
                    positiveSamples.Add(positiveSamples[i % positiveSamples.Count]); // Basit örnekleme
                }
            }
            else if (negativeSamples.Count < positiveSamples.Count)
            {
                int difference = positiveSamples.Count - negativeSamples.Count;
                for (int i = 0; i < difference; i++)
                {
                    negativeSamples.Add(negativeSamples[i % negativeSamples.Count]); // Basit örnekleme
                }
            }

            return positiveSamples.Concat(negativeSamples).ToList();
        }

        // K-Fold Cross Validation
        public void KFoldCrossValidation(List<EmgData> data, int k = 5)
        {
            var shuffledData = data.OrderBy(a => Guid.NewGuid()).ToList();
            int foldSize = data.Count / k;

            for (int fold = 0; fold < k; fold++)
            {
                var validationData = shuffledData.Skip(fold * foldSize).Take(foldSize).ToList();
                var trainingData = shuffledData.Except(validationData).ToList();

                // Model eğitimi
                ML_TrainModel(trainingData);
            }
        }

        // Özellik mühendisliği için bazı örnek metrikleri hesaplıyoruz (Zaman alanı özellikleri)
        public void FeatureEngineering(List<EmgData> data)
        {
            foreach (var item in data)
            {
                //// Örnek: Mean ve RMS gibi zaman alanı özellikleri ekleyin
                //float mean1 = item.Feature1_vector.Average();
                //float mean2 = item.Feature2_vector.Average();
                //float rms1 = (float)Math.Sqrt(item.Feature1_vector.Select(x => x * x).Average());
                //float rms2 = (float)Math.Sqrt(item.Feature2_vector.Select(x => x * x).Average());

                //// Özellik mühendisliği sonuçlarını vektöre dahil edin
                //item.Feature1_vector = item.Feature1_vector.Concat(new float[] { mean1, rms1 }).ToArray();
                //item.Feature2_vector = item.Feature2_vector.Concat(new float[] { mean2, rms2 }).ToArray();
            }
        }

        public float CalculateStandardDeviation_ml(List<float> values)
        {
            double average = values.Average();
            double sumOfSquaresOfDifferences = values.Select(val => (val - average) * (val - average)).Sum();
            double standardDeviation = Math.Sqrt(sumOfSquaresOfDifferences / values.Count);
            return (float)standardDeviation;
        }

    }
}
