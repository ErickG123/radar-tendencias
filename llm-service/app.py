from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from transformers import pipeline

app = FastAPI(title="Radar de Tendências - LLM Service")

generator = pipeline("text-generation", model="Qwen/Qwen2.5-1.5B-Instruct", device_map="auto")

class PromptRequest(BaseModel):
    franquia: str
    hype: float
    sentimento: float
    palavras: list[str]

class SummaryResponse(BaseModel):
    resumo: str

@app.post("/generate-summary", response_model=SummaryResponse)
def generate_summary(request: PromptRequest):
    try:
        palavras_str = ", ".join(request.palavras)
        
        messages = [
            {"role": "system", "content": "Você é um analista de mercado de entretenimento. Gere um parágrafo curto e direto (máximo 3 linhas) em português resumindo a situação da franquia com base nos dados. Não use formatação markdown."},
            {"role": "user", "content": f"Franquia: {request.franquia}\nHype Score: {request.hype}/100\nSentimento: {request.sentimento}%\nTópicos: {palavras_str}"}
        ]
        
        output = generator(messages, max_new_tokens=100, temperature=0.7)
        generated_text = output[0]['generated_text'][-1]['content'].strip()
        
        return SummaryResponse(resumo=generated_text)
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

if __name__ == "__main__":
    import uvicorn
    uvicorn.run(app, host="0.0.0.0", port=5001)