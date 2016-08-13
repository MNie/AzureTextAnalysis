#r "../packages/FSharp.Data.2.3.2/lib/net40/FSharp.Data.dll"
#r "../packages/Newtonsoft.Json.6.0.5/lib/net40/Newtonsoft.Json.dll"

open FSharp.Data
open Newtonsoft.Json
open System.IO
open FSharp.Data.HttpRequestHeaders

type commentaries = CsvProvider<"input.csv">

type responseJson =
    {
        documents: seq<responseDoc>
        errors: string list
    }
and responseDoc =
    {
        score: double
        id: string
    }

type requestJson =
    {
        documents: seq<document>
    }
and document =
    {
        language: string
        id: string
        text: string
    }

type results =
    {
        sentiments: estimatedDoc list
    }
and estimatedDoc =
    {
        Id: string
        Text: string
        Score: double
    }

let createJson (data:seq<CsvProvider<"input.csv">.Row>) =
    {
        documents = data |> Seq.map ( fun x -> x.ToString()) |> Seq.mapi ( fun i x -> {language = "en"; id = i.ToString(); text = x})
    }
    

let saveJson (json, fileName: string) =
    use outFile = new StreamWriter(fileName)
    (
        outFile.Write(JsonConvert.SerializeObject json)
    )

let getSentimentScore json =
    let serializeJson = JsonConvert.SerializeObject json
    let response = Http.RequestString("https://westus.api.cognitive.microsoft.com/text/analytics/v2.0/sentiment",
        body = TextRequest serializeJson,
        headers = [ContentType HttpContentTypes.Json; "Ocp-Apim-Subscription-Key", "api_key"])
    response

let concatSentimentAndInput = 
    let json = createJson (commentaries.Load("input.csv").Rows)
    saveJson (json, "")//Here should be a path where to save request json
    let estimate = getSentimentScore json
    saveJson (estimate, "")//Here should be a path where to save sentiment json
    let parsedSentiment = JsonConvert.DeserializeObject<responseJson>(estimate)
    parsedSentiment.documents
    |> Seq.map (fun x -> 
        {
            Id = x.id; 
            Text = json.documents 
                |> Seq.where (fun y -> y.id.Equals x.id) 
                |> Seq.head 
                |> (fun y -> y.text); 
            Score = x.score
        }
    )
    |> Seq.sortBy (fun x -> x.Score)

saveJson (concatSentimentAndInput, "")//Here should be a path where to save result json
