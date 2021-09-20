using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Threads
{
    class Program
    {
        private static int ThreadCounts = 5;
        private static ManualResetEvent WordsProcessDone = new(false);
        private static Dictionary<string, string> Sentences = new();
        public static ConcurrentDictionary<string, int> WordDictionary = new();

        static void Main(string[] args)
        {

            // TXT file ın okunması
            var paragraph = File.ReadAllText(args[0]);


            // Tread Count un ekrandan verilmesi
            SetTreadLineValue();

            //Dosyadaki cümle ve kelime sayılarının belirlenmesi
            var sentenceStartIndex = 0;
            var wordCount = String.IsNullOrEmpty(paragraph) ? 0 : 1;
            for (var p = 0; p < paragraph.Length; p++)
            {
                var currentChar = paragraph[p];


                switch (currentChar)
                {
                    case ' ':
                        wordCount++;
                        break;
                    case '.':
                    case '?':
                    case '!':
                        {
                            var sentence = paragraph.Substring(sentenceStartIndex, p - sentenceStartIndex);
                            sentenceStartIndex = p + 1;
                            Sentences[sentence] = sentence;
                            break;
                        }
                }
            }

            if (wordCount == 0)
            {
                Console.WriteLine("No sentences(s) found");
                Console.ReadLine();
            }

            Console.WriteLine($"Total Sentences -> {Sentences.Count}");
            Console.WriteLine($"Total words -> {wordCount}");

            var workers = new List<SentenceProcessor>();

            //belirlenene sayıda thread ların oluşturulması
            for (var j = 0; j < ThreadCounts; j++)
            {
                var worker = new SentenceProcessor();

                //herbir thread için, süreçlerinin tamamlanmasının yakalanması için event in oluşturulması
                worker.SentenceProcessed += WorkerOnSentenceProcessed;
                workers.Add(worker);
                Task.Run(() => worker.Start());
            }

            // Cümlelerin thread lara paylaştırılması
            for (var i = 0; i < Sentences.Count; i++)
            {
                var currentWorker = (i % workers.Count);
                var worker = workers.ElementAt(currentWorker);
                var sentence = Sentences.Keys.ElementAt(i);
                worker.AddSentence(sentence);
            }

            // bu aşamada alt thread lra cümlelerini işlediği sürece ana thread bekletilmekte
            WordsProcessDone.WaitOne();

            //Event lerin düşürülmesi ve thread ların kapatılması
            workers.ForEach(f =>
            {
                f.SentenceProcessed -= WorkerOnSentenceProcessed;
                f.Stop();
            });

            //kelimelerin sıralanması
            var sorted = WordDictionary
                .OrderByDescending(o => o.Value)
                .ThenByDescending(t => t.Key);

            //kelimeleri  console a yazdırılması
            foreach (var (key, value) in sorted)
            {
                Console.WriteLine($"{key} - {value}");
            }

            Console.ReadLine();
        }


        public static void SetTreadLineValue()
        {
            Console.WriteLine("Please insert TreadCount value:");
            string sThreadCounts = Console.ReadLine();
            if (!Int32.TryParse(sThreadCounts, out ThreadCounts))
            {
                Console.WriteLine("Please insert only numeric value for TreadCount value:");
                SetTreadLineValue();
            }
        }

        private static void WorkerOnSentenceProcessed(object sender, SentenceProcessedArg e)
        {

            lock (Sentences)
            {
                if (e.Words != null)
                { //her alt threadın işini tamamlanması ardından Dictionary de bu kelimelerin toplanması
                    foreach (var word in e.Words)
                    {
                        WordDictionary.TryGetValue(word, out var count);
                        WordDictionary[word] = count + 1;
                    }
                }

                Sentences.Remove(e.Sentence, out _);
                // Tüm thread lar işini bitirdiğinde ana thread e "devam" sinyalinin verilmesi
                if (Sentences.Count == 0)
                {
                    WordsProcessDone.Set();
                }
            }
        }
    }
}