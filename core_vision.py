import cv2
import socket
import json
import math
import mediapipe as mp

# ---------------------------------------------------------
# Vision Engine: Dual-Track Initialization
# ---------------------------------------------------------
mp_hands = mp.solutions.hands
mp_draw = mp.solutions.drawing_utils

UDP_IP = "127.0.0.1"
UDP_PORT = 5052
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

hands = mp_hands.Hands(max_num_hands=2, min_detection_confidence=0.7, min_tracking_confidence=0.7)

# ---------------------------------------------------------
# THE HARDWARE OVERRIDE (DirectShow API)
# ---------------------------------------------------------
# 'cv2.CAP_DSHOW' bypasses Windows throttling. 
cap = cv2.VideoCapture(0, cv2.CAP_DSHOW)

# Force optimal calculation resolution to prevent rendering lag
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

print("DirectShow Dual-Sensor Array Online. Rendering Matrix GUI...")

while cap.isOpened():
    success, img = cap.read()
    if not success:
        break

    img_rgb = cv2.cvtColor(img, cv2.COLOR_BGR2RGB)
    results = hands.process(img_rgb)

    telemetry_data = {"hands": []}

    if results.multi_hand_landmarks:
        for hand_landmarks in results.multi_hand_landmarks:
            
            x4, y4 = hand_landmarks.landmark[4].x, hand_landmarks.landmark[4].y
            x8, y8 = hand_landmarks.landmark[8].x, hand_landmarks.landmark[8].y

            distance = math.sqrt((x8 - x4)**2 + (y8 - y4)**2)
            gesture_state = "pinch" if distance < 0.04 else "open"

            telemetry_data["hands"].append({
                "gesture": gesture_state,
                "wrist_x": hand_landmarks.landmark[0].x,
                "wrist_y": hand_landmarks.landmark[0].y
            })

            mp_draw.draw_landmarks(img, hand_landmarks, mp_hands.HAND_CONNECTIONS)

    sock.sendto(json.dumps(telemetry_data).encode(), (UDP_IP, UDP_PORT))
    
    # Re-enabled the visual matrix for your presentation
    cv2.imshow("AR Dual Sensor Matrix", img)
    
    if cv2.waitKey(1) & 0xFF == ord('q'):
        break

cap.release()
cv2.destroyAllWindows()