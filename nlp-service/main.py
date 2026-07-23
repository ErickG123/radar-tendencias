from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from transformers import pipeline
import re
from collections import Counter

app = FastAPI(title="Radar de Tendências - NLP Service")

try:
    sentiment_pipeline = pipeline(
        "sentiment-analysis",
        model="cardiffnlp/twitter-xlm-roberta-base-sentiment"
    )
except Exception:
    sentiment_pipeline = pipeline(
        "sentiment-analysis",
        model="distilbert-base-uncased-finetuned-sst-2-english"
    )


class TextRequest(BaseModel):
    text: str


class SentimentResponse(BaseModel):
    score: float
    label: str

class KeywordResult(BaseModel):
    word: str
    weight: int
    sentiment: str

class ExtractionResponse(BaseModel):
    keywords: list[KeywordResult]

STOP_WORDS = {"de", "a", "o", "que", "e", "do", "da", "em", "um", "para", "é", "com", "não", "uma", "os", "no", "se", "na", "por", "mais", "as", "dos", "como", "mas", "foi", "ao", "ele", "das", "tem", "à", "seu", "sua", "ou", "ser", "quando", "muito", "nos", "já", "está", "eu", "também", "só", "pelo", "pela", "até", "isso", "ela", "entre", "era", "depois", "sem", "mesmo", "aos", "ter", "seus", "quem", "nas", "me", "esse", "eles", "estão", "você", "tinha", "foram", "essa", "num", "nem", "suas", "meu", "às", "minha", "têm", "numa", "pelos", "elas", "havia", "seja", "qual", "será", "nós", "tenho", "lhe", "deles", "essas", "esses", "pelas", "este", "fosse", "dele"}

@app.post("/analyze", response_model=SentimentResponse)
def analyze_sentiment(request: TextRequest):
    if not request.text or len(request.text.strip()) == 0:
        return SentimentResponse(score=50.0, label="neutral")

    try:
        result = sentiment_pipeline(
            request.text, truncation=True, max_length=512)[0]

        label = result['label'].lower()
        confidence = result['score']

        if "positive" in label:
            score = 50.0 + (confidence * 50.0)
        elif "negative" in label:
            score = 50.0 - (confidence * 50.0)
        else:
            score = 50.0

        return SentimentResponse(score=score, label=label)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/extract-keywords", response_model=ExtractionResponse)
def extract_keywords(request: TextRequest):
    if not request.text or len(request.text.strip()) == 0:
        return ExtractionResponse(keywords=[])
    
    try:
        clean_text = re.sub(r'[^\w\s]', '', request.text.lower())
        words = [w for w in clean_text.split() if w not in STOP_WORDS and len(w) > 3]
        word_counts = Counter(words).most_common(15)
        
        results = []
        for word, count in word_counts:
            sent_result = sentiment_pipeline(word, truncation=True, max_length=512)[0]
            label = sent_result['label'].lower()
            
            sentiment_category = "neutral"
            if "positive" in label:
                sentiment_category = "positive"
            elif "negative" in label:
                sentiment_category = "negative"
                
            results.append(KeywordResult(word=word, weight=count, sentiment=sentiment_category))
            
        return ExtractionResponse(keywords=results)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))


if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=5000)
