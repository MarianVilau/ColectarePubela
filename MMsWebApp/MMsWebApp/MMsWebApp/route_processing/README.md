# Route Processing Module

Acest modul este responsabil pentru generarea matricei de distanțe și durate între punctele de colectare a deșeurilor folosind Mapbox Matrix API.

## Structură

```
route_processing/
├── mapbox_matrix_generator.py  # Script principal pentru generarea matricei
├── requirements.txt           # Dependințele Python necesare
├── .env                      # Configurație Mapbox API Token
├── mapbox_matrix.log         # Fișier de log pentru monitorizare
└── matrix_results/           # Director cu rezultatele generate
    └── full_matrix_mapbox.json # Matricea completă generată
```

## Instalare

1. Creează un mediu virtual Python:
```bash
python -m venv venv
```

2. Activează mediul virtual:
```bash
# Windows
venv\Scripts\activate

# Linux/MacOS
source venv/bin/activate
```

3. Instalează dependințele:
```bash
pip install -r requirements.txt
```

## Configurare Mapbox

1. Creează un cont Mapbox și obține un API key de la: https://account.mapbox.com/
2. Creează un fișier `.env` în directorul rădăcină cu conținutul:
```
MAPBOX_ACCESS_TOKEN=your_mapbox_access_token_here
```

## Generare Matrice

Pentru a genera matricea completă de distanțe și durate:

```bash
python mapbox_matrix_generator.py
```

Scriptul va afișa progresul în consolă în timp real:
```
=== Citire coordonate din CSV ===
✓ S-au citit 150 coordonate unice din CSV
✓ Date împărțite în 6 chunk-uri

=== Procesare chunk-uri ===
Total operații de procesat: 36
Progres: 33.3% (12/36) - Procesare chunk-uri 2/6 -> 4/6
```

La final, vor fi afișate statistici complete:
```
=== Statistici finale ===
• Timp total de procesare: 245.32 secunde
• Puncte procesate: 150
• Chunk-uri procesate: 6
• Operații totale: 36
• Dimensiune matrice: 150x150
```

## Structura Rezultatelor

Fișierul `full_matrix_mapbox.json` conține:

```json
{
  "coordinates": [
    [longitude, latitude],
    // ... toate coordonatele unice
  ],
  "durations": [
    [0, 573, 1169.5],    // timpul în secunde între puncte
    // ... matrice NxN completă
  ],
  "distances": [
    [0, 1500, 3000],     // distanța în metri între puncte
    // ... matrice NxN completă
  ],
  "metadata": {
    "total_points": 150,
    "total_chunks": 6,
    "timestamp": "2024-03-21 10:30:00",
    "processing_time_seconds": 245.32
  }
}
```

## Monitorizare și Logging

Scriptul oferă două tipuri de monitorizare:
1. **În consolă**: Afișează progresul în timp real, inclusiv:
   - Numărul de coordonate citite
   - Progresul procesării (procent și numere absolute)
   - Statistici finale
   - Erori și avertismente

2. **În fișierul de log**: Salvează toate operațiile în `mapbox_matrix.log` pentru referință ulterioară

## Note

- Scriptul respectă limita de 25 de puncte per request a API-ului Mapbox
- Include o pauză de 1 secundă între request-uri pentru a respecta rate limiting
- Generează o matrice completă NxN cu toate distanțele și duratele între puncte
- Salvează metadata utilă despre procesare în rezultatul final 