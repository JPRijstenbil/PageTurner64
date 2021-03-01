// Reading of the switches & sending reads to pc via serial

void setup() {
  Serial.begin(9600);
  pinMode(2, INPUT);      // connect to ground via switch 1
  pinMode(11, INPUT);     // connect to ground via switch 2
  pinMode(LED_BUILTIN, OUTPUT);
  delay(2000);
}

void loop() {
  bool forward = digitalRead(2);
  bool backward = digitalRead(11);

  if (forward == true)
  {
    Serial.write(1);
    digitalWrite(LED_BUILTIN, HIGH);
    delay(300);
    digitalWrite(LED_BUILTIN, LOW);
  }
  else if (backward == true)
  {
    Serial.write(0);
    digitalWrite(LED_BUILTIN, HIGH);
    delay(300);
    digitalWrite(LED_BUILTIN, LOW);
  }
}
