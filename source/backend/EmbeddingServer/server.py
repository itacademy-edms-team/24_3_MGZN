from fastapi import FastAPI, HTTPException
from pydantic import BaseModel
from sentence_transformers import SentenceTransformer
import logging
import uvicorn

# --- Настройка логирования ---
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# --- Pydantic-модель для тела запроса ---
class TextInput(BaseModel):
    text: str

# --- Pydantic-модель для ответа ---
class EmbeddingResponse(BaseModel):
    embedding: list[float]

# --- Инициализация FastAPI приложения ---
app = FastAPI(
    title="Embedding API",
    description="API для генерации векторов (embeddings) с помощью модели SentenceTransformer.",
    version="1.0.0"
)

# --- Загрузка модели при старте сервера ---
MODEL_NAME = "cointegrated/LaBSE-en-ru"
logger.info(f"Загрузка модели '{MODEL_NAME}'...")
model = SentenceTransformer(MODEL_NAME)
logger.info(f"Модель '{MODEL_NAME}' загружена успешно.")

# --- Определение эндпоинта ---
@app.post("/embed", response_model=EmbeddingResponse)
async def get_embedding(input_data: TextInput):
    """
    Принимает текст и возвращает его векторное представление (embedding).
    """
    try:
        logger.debug(f"Получен запрос на векторизацию: '{input_data.text[:50]}...'") # Логируем начало
        embedding = model.encode([input_data.text], convert_to_numpy=True)[0].astype('float32').tolist()
        logger.debug(f"Векторизация завершена для: '{input_data.text[:50]}...'")
        return EmbeddingResponse(embedding=embedding)
    except Exception as e:
        logger.error(f"Ошибка при векторизации текста '{input_data.text}': {e}")
        raise HTTPException(status_code=500, detail=f"Ошибка при генерации вектора: {str(e)}")

# --- Точка входа для запуска сервера ---
if __name__ == "__main__":
    # uvicorn запускает сервер
    # host и port можно изменить
    uvicorn.run(app, host="127.0.0.1", port=8000, log_level="info")