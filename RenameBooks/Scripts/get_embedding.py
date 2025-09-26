# Scripts/get_embedding.py
import sys
import json
from sentence_transformers import SentenceTransformer

# Загружаем модель (кэшируется автоматически в ~/.cache/torch/sentence_transformers)
model = SentenceTransformer('sentence-transformers/paraphrase-multilingual-MiniLM-L12-v2')

# Читаем текст из stdin
input_text = sys.stdin.read().strip()

if not input_text:
    sys.exit(1)

# Получаем эмбеддинг
embedding = model.encode([input_text], convert_to_numpy=True)[0]

# Выводим как JSON-массив чисел
print(json.dumps(embedding.tolist()))