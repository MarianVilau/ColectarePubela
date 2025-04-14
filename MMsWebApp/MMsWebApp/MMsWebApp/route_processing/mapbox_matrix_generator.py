import os
import json
import pandas as pd
import requests
from dotenv import load_dotenv
import logging
from typing import List, Dict, Tuple
import time
import numpy as np
from datetime import datetime

# Configurare logging pentru fișier și consolă
logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s - %(levelname)s - %(message)s',
    handlers=[
        logging.FileHandler('mapbox_matrix.log'),
        logging.StreamHandler()
    ]
)

# Încărcare variabile de mediu
load_dotenv()
MAPBOX_TOKEN = os.getenv('MAPBOX_ACCESS_TOKEN')

def validate_coordinates(lon: float, lat: float) -> bool:
    """Validează coordonatele conform limitelor Mapbox."""
    return -180 <= lon <= 180 and -90 <= lat <= 90

def read_coordinates_from_csv(file_path: str) -> List[Tuple[float, float]]:
    """Citește coordonatele din fișierul CSV și returnează lista de coordonate unice."""
    try:
        print("\n=== Citire coordonate din CSV ===")
        df = pd.read_csv(file_path)
        # Eliminăm rândurile unde Latitude sau Longitude sunt NaN
        df = df.dropna(subset=['Latitude', 'Longitude'])
        
        # Validăm și curățăm coordonatele
        valid_coords = []
        invalid_coords = []
        
        for _, row in df.iterrows():
            lon, lat = float(row['Longitude']), float(row['Latitude'])
            if validate_coordinates(lon, lat):
                valid_coords.append([lon, lat])
            else:
                invalid_coords.append([lon, lat])
        
        if invalid_coords:
            print(f"⚠ S-au găsit {len(invalid_coords)} coordonate invalide care vor fi ignorate:")
            for coord in invalid_coords[:3]:  # Afișăm primele 3 coordonate invalide
                print(f"  • {coord}")
            if len(invalid_coords) > 3:
                print(f"    ... și încă {len(invalid_coords) - 3}")
        
        # Eliminăm duplicatele
        unique_coords = list(map(list, set(map(tuple, valid_coords))))
        print(f"✓ S-au citit {len(unique_coords)} coordonate unice valide din CSV")
        return unique_coords
    except Exception as e:
        print(f"✗ Eroare la citirea fișierului CSV: {str(e)}")
        raise

def chunk_coordinates(coordinates: List[List[float]], chunk_size: int = 24) -> List[List[List[float]]]:
    """Împarte lista de coordonate în chunk-uri mai mici pentru a respecta limitele API-ului."""
    if chunk_size > 24:
        print("⚠ Atenție: chunk_size redus la 24 pentru a respecta limitele API-ului")
        chunk_size = 24
        
    chunks = [coordinates[i:i + chunk_size] for i in range(0, len(coordinates), chunk_size)]
    total_chunks = len(chunks)
    total_operations = total_chunks * total_chunks
    
    print(f"\n=== Informații chunk-uri ===")
    print(f"• Coordonate totale: {len(coordinates)}")
    print(f"• Dimensiune chunk: {chunk_size}")
    print(f"• Număr chunk-uri: {total_chunks}")
    print(f"• Operații necesare: {total_operations}")
    print(f"• Timp estimat: ~{total_operations} secunde (1 request/secundă)")
    
    if total_operations > 3600:
        print("\n⚠ ATENȚIE: Procesarea va dura mai mult de o oră!")
        print("   Considerați reducerea numărului de coordonate sau creșterea dimensiunii chunk-ului.")
    
    return chunks

def get_matrix_chunk(sources: List[List[float]], destinations: List[List[float]]) -> Dict:
    """Obține matricea de distanțe pentru un set de coordonate sursă și destinație."""
    base_url = "https://api.mapbox.com/directions-matrix/v1/mapbox/driving"
    
    try:
        # Verificăm limitele API-ului
        total_coords = len(sources) + len(destinations)
        if total_coords > 25:
            raise ValueError(f"Numărul total de coordonate ({total_coords}) nu poate depăși 25")
            
        # Formatăm coordonatele pentru URL (longitude,latitude)
        coords_str = ';'.join([f"{coord[0]},{coord[1]}" for coord in sources + destinations])
        
        # Construim URL-ul de bază
        url = f"{base_url}/{coords_str}"
        
        # Parametrii de bază
        params = {
            'access_token': MAPBOX_TOKEN,
            'annotations': 'duration,distance'
        }
        
        # Adăugăm sources și destinations doar dacă nu folosim toate coordonatele
        if len(sources) != len(destinations):
            params['sources'] = ';'.join(str(i) for i in range(len(sources)))
            params['destinations'] = ';'.join(str(i) for i in range(len(sources), len(sources) + len(destinations)))
        
        # Facem request-ul
        print(f"Procesare: {len(sources)} surse -> {len(destinations)} destinații")
        response = requests.get(url, params=params)
        
        if response.status_code != 200:
            error_msg = f"Status: {response.status_code}"
            if response.text:
                error_msg += f", Răspuns: {response.text[:200]}"
            raise requests.exceptions.RequestException(error_msg)
        
        result = response.json()
        if result.get('code') != 'Ok':
            raise Exception(f"Eroare API Mapbox: {result.get('message', 'Eroare necunoscută')}")
        
        return result
        
    except requests.exceptions.RequestException as e:
        print(f"✗ Eroare request: {str(e)}")
        raise
    except Exception as e:
        print(f"✗ Eroare: {str(e)}")
        raise

def process_matrix(csv_path: str, output_path: str):
    """Procesează toate coordonatele și generează matricea completă."""
    start_time = time.time()
    print("\n=== Începere procesare matrice ===")
    
    coordinates = read_coordinates_from_csv(csv_path)
    total_points = len(coordinates)
    
    # Inițializăm matricele complete
    full_duration_matrix = np.zeros((total_points, total_points))
    full_distance_matrix = np.zeros((total_points, total_points))
    
    # Împărțim coordonatele în chunk-uri de 12 puncte
    source_chunks = [coordinates[i:i + 12] for i in range(0, len(coordinates), 12)]
    dest_chunks = [coordinates[i:i + 12] for i in range(0, len(coordinates), 12)]
    
    total_source_chunks = len(source_chunks)
    total_dest_chunks = len(dest_chunks)
    total_operations = total_source_chunks * total_dest_chunks
    current_operation = 0
    
    print(f"\n=== Procesare chunk-uri ===")
    print(f"Total operații de procesat: {total_operations}")
    print(f"Puncte per chunk: 12 surse + 12 destinații")
    print(f"Timp estimat: ~{total_operations} secunde (1 request/secundă)")
    
    # Pentru fiecare pereche de chunk-uri
    for i, source_chunk in enumerate(source_chunks):
        for j, dest_chunk in enumerate(dest_chunks):
            current_operation += 1
            progress = (current_operation / total_operations) * 100
            
            print(f"\rProgres: {progress:.1f}% ({current_operation}/{total_operations}) - "
                  f"Procesare chunk-uri {i+1}/{total_source_chunks} -> {j+1}/{total_dest_chunks}", end="")
            
            try:
                result = get_matrix_chunk(source_chunk, dest_chunk)
                
                # Calculăm pozițiile în matricea completă
                start_row = i * 12
                end_row = min(start_row + len(source_chunk), total_points)
                start_col = j * 12
                end_col = min(start_col + len(dest_chunk), total_points)
                
                # Extragem matricile de durate și distanțe din răspuns
                durations = np.array(result['durations'])
                distances = np.array(result['distances'])
                
                # Extragem doar submatricea relevantă
                sub_durations = durations[:len(source_chunk), :len(dest_chunk)]
                sub_distances = distances[:len(source_chunk), :len(dest_chunk)]
                
                # Actualizăm matricele complete
                full_duration_matrix[start_row:end_row, start_col:end_col] = sub_durations
                full_distance_matrix[start_row:end_row, start_col:end_col] = sub_distances
                
                # Așteptăm 1 secundă între request-uri
                time.sleep(1)
            except Exception as e:
                print(f"\n✗ Eroare la procesarea chunk-urilor {i+1}->{j+1}: {str(e)}")
                continue
    
    print("\n\n=== Salvare rezultate ===")
    # Salvăm rezultatele
    try:
        result_data = {
            'coordinates': coordinates,
            'durations': full_duration_matrix.tolist(),
            'distances': full_distance_matrix.tolist(),
            'metadata': {
                'total_points': total_points,
                'total_source_chunks': total_source_chunks,
                'total_dest_chunks': total_dest_chunks,
                'timestamp': datetime.now().strftime('%Y-%m-%d %H:%M:%S'),
                'processing_time_seconds': round(time.time() - start_time, 2)
            }
        }
        
        # Creăm directorul dacă nu există
        os.makedirs(os.path.dirname(output_path), exist_ok=True)
        
        with open(output_path, 'w', encoding='utf-8') as f:
            json.dump(result_data, f, indent=2)
        print(f"✓ Rezultatele au fost salvate în {output_path}")
        
        # Afișăm statistici finale
        print("\n=== Statistici finale ===")
        print(f"• Timp total de procesare: {round(time.time() - start_time, 2)} secunde")
        print(f"• Puncte procesate: {total_points}")
        print(f"• Chunk-uri sursă: {total_source_chunks}")
        print(f"• Chunk-uri destinație: {total_dest_chunks}")
        print(f"• Operații totale: {total_operations}")
        print(f"• Dimensiune matrice: {total_points}x{total_points}")
        
    except Exception as e:
        print(f"✗ Eroare la salvarea rezultatelor: {str(e)}")
        raise

if __name__ == "__main__":
    csv_path = "../Data/Traseu2.csv"
    output_path = "matrix_results/full_matrix_mapbox.json"
    
    try:
        process_matrix(csv_path, output_path)
        print("\n✓ Procesare completă!")
    except Exception as e:
        print(f"\n✗ Eroare în timpul procesării: {str(e)}") 