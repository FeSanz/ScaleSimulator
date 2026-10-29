# **CBCP-03** 

**Protocolo de comunicación Radwag** Indicador de pesaje PUE 7.1 Indicador de pesaje PUE HY10 Balanzas de precisión WLY Basculas multifuncionales WPY Basculas multifuncionales HY10 

MANUAL DE USUARIO 

ITKU-07-01-12-18-ES 

www.radwag.com 

DICIEMBRE 2018 

2 

## **ÍNDICE** 

|**1.** **DATOS BÁSICOS .......................................................................................................................................... 4**|
|---|
|**2.** **JUEGO DE COMANDO ................................................................................................................................. 4**|
|**3.** **FORMATO DE LA RESPUESTA A LA PREGUNTA DEL ORDENADOR ..................................................... 5**<br>|
|**4.** **DESCRIPCIÓN DEL COMANDO ................................................................................................................... 5**<br>|
|4.1. Puesta a cero .......................................................................................................................................... 5|
|4.2. Tara ......................................................................................................................................................... 6|
|4.3. Poner el valor de tara .............................................................................................................................. 6|
|4.4. Ajustar tara .............................................................................................................................................. 6<br>|
|4.5. Poner el resultado estable en la unidad básica ....................................................................................... 7<br>|
|4.6. Introducir el resultado inmediatamente en la unidad básica .................................................................... 7<br>|
|4.7. Introducir el resultado de todas las plataformas inmediatamente en unidades básicas. .......................... 8|
|4.8. Introducir el resultado estable en la unidad actual ................................................................................... 8|
|4.9. Poner el resultado inmediatamente en la unidad actual .......................................................................... 9|
|4.10. Activar la transmisión continua en la unidad básica .............................................................................. 9|
|4.11. Desconectar la transmisión continúa en la unidad básica ................................................................... 10|
|4.12. Conectar la transmisión continúa en la unidad actual .......................................................................... 10|
|4.13. Desconectar la transmisión continúa en la unidad actual .................................................................... 10|
|4.14. Ajuste el umbral inferior de verificación de peso (controlador) ............................................................ 10|
|4.15. Ajuste el umbral superior de verificación de peso (controlador) .......................................................... 11<br>|
|4.16. Poner el valor de umbral inferior de verificación de peso (controlador) ............................................... 11<br>|
|4.17. Poner el valor de umbral superior de verificación de peso (controlador) ............................................. 11|
|4.18. Simulación de presionar el botón ENTER / PRINT .............................................................................. 11|
|4.19. Cambiar la unidad ............................................................................................................................... 12|
|4.20. Introducir el número de fabrica ............................................................................................................ 12|
|4.21. Ajustar la masa de pieza ..................................................................................................................... 12|
|4.22. Introducir la masa de referencia .......................................................................................................... 12|
|4.23. Activar la señal de sonido ................................................................................................................... 13|
|4.24. Introducir modos de trabajo disponibles .............................................................................................. 13|
|4.25. Ajustar modo de trabajo ...................................................................................................................... 14|
|4.26. Introducir modo de trabajo actual ........................................................................................................ 15|
|4.27. Enviar todos los comandos aplicados ................................................................................................. 15<br>|
|**5.** **Impresión manual/ Impresión automática ................................................................................................ 16**|



3 

## **1. DATOS BÁSICOS** 

- A. El protocolo de comunicación la balanza - indicador sirve para la comunicación entra la balanza RADWAG y el dispositivo externo usando:RS-232C, Ethernet. 

- B. Protocolo consta de comando enviados de un dispositivo externo a la balanza y la respuesta de la balanza al dispositivo 

- C. Las respuestas se envían de balanza cada vez, después de recibir comando como la respuesta al comando dado. 

- D. Con los comandos que componen el protocolo de comunicación se puede obtener información sobre el estado de la balanza y afectar a su funcionamiento, por ejemplo: recibir resultados de pesaje de la balanza, puesta a cero, etc. 

## **2. JUEGO DE COMANDO** 

|**Comando**|**Descripción**|
|---|---|
|**Z**|Puesta a cero de balanza|
|**T**|Tara|
|**OT**|Poner el valor de tara|
|**UT**|Ajustar tara|
|**S**|Poner el resultado estable en la unidad básica|
|**SI**|Introducir el resultado inmediatamente en la unidad básica|
|**SIA**|Introducir el resultado de todas las plataformas inmediatamente en<br>unidades básicas.|
|**SU**|Introducir el resultado estable en la unidad actual|
|**SUI**|Poner el resultado inmediatamente en la unidad actual|
|**C1**|Activar la transmisión continua en la unidad básica|
|**C0**|Desconectar la transmisión continúa en la unidad básica|
|**CU1**|Conectar la transmisión continúa en la unidad actual|
|**CU0**|Desconectar la transmisión continúa en la unidad actual|
|**DH**|Ajuste el umbral inferior de verificación depeso (controlador)|
|**UH**|Ajuste el umbral superior de verificación depeso (controlador)|
|**ODH**|Poner el valor de umbral inferior de verificación depeso (controlador)|
|**OUH**|Poner el valor de umbral superior de verificación depeso (controlador)|
|**SS**|Simulación depresionar el botón ENTER / PRINT|
|**P**|Cambiar la unidad|
|**NB**|Introducir el número de fabrica|
|**SM**|Ajustar la masa depieza|



4 

|**RM**|Introducir la masa de referencia|
|---|---|
|**BP**|Activar la señal de sonido|
|**OMI**|Introducir modos de trabajo disponibles|
|**OMS**|Ajustar modo de trabajo|
|**OMG**|Introducir modo de trabajo actual|
|**PC**|Enviar todos los comandos aplicados|
||**_Cada comando debe ser terminado a los signos CR LF._**|



## **3. FORMATO DE LA RESPUESTA A LA PREGUNTA DEL ORDENADOR** 

Indicador después del comando, responde: 

|**XX_A CR LF**|comando entendido, comenzó a realizar|
|---|---|
|**XX_D CR LF**|Comando se terminó (ocurre sólo después de XX_A)|
|**XX_I CR LF**|comando entendido,pero en el momento no está disponible|
|**XX _ ^ CR LF**|comando entendido,pero se ha superado el rango máximo|
|**XX _ v CR LF**|comando entendido,pero se ha superado el rango mínimo.|
|**XX _ OK CR LF**|comando realizado|
|**ES_CR LF**|comando no entendido.|
|**XX _ E CR LF**|límite de tiempo superado en espera del resultado de la estabilidad<br>(límite de tiempo es elparámetro característico de balanza)|



**XX** en cada caso, es el nombre del comando enviado. 

_ - representa un carácter de espacio (el espacio). 

## **4. DESCRIPCIÓN DEL COMANDO** 

### **4.1. Puesta a cero** 

Secuencia: **Z CR LF** 

Respuestas posibles: 

|**Z_A CR LF**<br>**Z_D CR LF**|- comando entendido, comenzó a realizar<br>- comando se terminó.|
|---|---|
|**Z_A CR LF**|- comando entendido, comenzó a realizar|
|**Z_^ CR LF**|- comando entendido,pero se ha superado el rango depuesta a cero.|
|**Z_A CR LF**|- comando entendido, comenzó a realizar|
|**Z_E CR LF**|- límite de tiempo superado en espera del resultado estable.|
|**Z_I CR LF**|- comando entendido, pero en el momento no está disponible.|



5 

### **4.2. Tara** 

Secuencia: **T CR LF** 

Respuestas posibles: 

|**T_A CR LF**<br>**T_D CR LF**|- comando entendido, comenzó a realizar<br>- comando se terminó.|
|---|---|
|**T_A CR LF**|- comando entendido, comenzó a realizar|
|**T_v CR LF**|- comando entendido,pero se ha superado el rango de tara.|
|**T_A CR LF**|- comando entendido, comenzó a realizar|
|**T_E CR LF**|- límite de tiempo superado en espera del resultado estable.|
|**T_I CR LF**|- comando entendido,pero en el momento no está disponible.|



### **4.3. Poner el valor de tara** 

Secuencia: **OT CR LF** 

Respuesta: **OT_TARA CR LF** - comando se terminó. 

Formato de respuestas: 

|1|2|3|4-12|13|14|15|16|17|18|19|
|---|---|---|---|---|---|---|---|---|---|---|
|O|T|espacio|Tara|espacio||Unidad||espacio|CR|LF|



**Tara** -  9 signos alineado a la derecha **Unidad** -  3 signos alineado a la izquierda 



### **_El valor de la tara se da siempre en la unidad de calibración._** 

### **4.4. Ajustar tara** 

Secuencia: **UT_TARA CR LF** , donde **TARA** – valor de tara 

Respuestas posibles: 

|**UT_OK CR LF**|- comando está hecho|
|---|---|
|**UT_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|**ES CR LF**|- comando no entendido (el formato de tara incorrecto)|





**_En formato de tara, hay que usar punto, como un marcador de decimales._** 

6 

### **4.5. Poner el resultado estable en la unidad básica** 

Secuencia: **S CR LF** 

Respuestas posibles: 

|**S_A CR LF**<br>**S_E CR LF**|- comando entendido, comenzó a realizar<br>- límite de tiempo superado en espera del resultado estable.|
|---|---|
|**S_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|**S_A CR LF**<br>**MARCO DE**<br>**MASA**|- comando entendido, comenzó a realizar<br>- valor de la masa se devuelve en la unidad básica|



Formato de marco de masa, que corresponde el indicador: 

|1|2-3|4|5|6|7-15|16|17|18|19|20|21|
|---|---|---|---|---|---|---|---|---|---|---|---|
|S|espacio|Signo<br>de<br>estabilidad|espacio|Signo|Masa|espacio||Unidad||CR|LF|



### **<u>Ejemplo:</u>** 

**S CR LF** – orden del ordenador 

S _ A CR LF - comando entendido y comenzó a realizar. 

**S _ _ _ _ - _ _ _ _ _ _ 8 . 5 _ g _ _ CR LF -** , comando se terminó, valor de la masa se devuelve en la unidad básica. 

### **4.6. Introducir el resultado inmediatamente en la unidad básica** 

Secuencia: **SI CR LF** 

Respuestas posibles: 

|**SI_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|---|---|
|**MARCO DE**|- el valor de la masa se devuelve en la unidad básica|
|**MASA**|inmediatamente.|



Formato de marco de masa, que corresponde el indicador: 

|1<br>2|3|4|5|6|7-15|16|17|18|19|20|21|
|---|---|---|---|---|---|---|---|---|---|---|---|
|S<br>I|espacio|Signo<br>de<br>estabilidad|espacio|Signo|Masa|espacio||Unidad||CR|LF|



### **<u>Ejemplo:</u>** 

**S I CR LF** – orden del ordenador. 

**S I _ ? _ _ _ _ _ _ _ 1 8 . 5 _ k g _ CR LF** - comando se terminó, valor de la masa se devuelve en la unidad básica inmediatamente. 

7 

### **4.7. Introducir el resultado de todas las plataformas inmediatamente en unidades básicas.** 

Secuencia: **SIA CR LF** 

Respuestas posibles: 

|**SIA_I CR LF**|- comando entendido, pero en el momento no está<br>disponible.|
|---|---|
|**RAMKA MASY „P1” CR LF**||
|**MARCO DE MASA „P2” CR**|-  valores de masa de todas las plataformas en la|
|**LF**|unidad base de cada plataforma se devuelven<br>inmediatamente.|



Formato de marco de masa de una plataforma particular, que corresponde el indicador: 

|1<br>2|3|4|5|6|7-15|16|17|18|19|20|21|
|---|---|---|---|---|---|---|---|---|---|---|---|
|P<br>N|espacio|Signo<br>de<br>estabilidad|espacio|Signo|Masa|espacio||Unidad||CR|LF|



**n** - número de plataforma **Masa** - 9 signos alineado a la derecha. **Unidad** - 3 signos alineado a la izquierda 

**<u>Ejemplo:</u>** 

Supongamos que tiene dos plataformas de pesaje conectadas: **S I A CR LF** – orden del ordenador. 

**P 1 _ ? _ _ _ _ _ _ 1 1 8 . 5 _ g _ _ CR LF P 2 _ _ _ _ _ _ _ _ _ 3 6 . 2 _ k g _ CR LF** - comando se terminó, valor de la masa se devuelve de los ambos plataformas en la unidad básica inmediatamente. 

### **4.8. Introducir el resultado estable en la unidad actual** 

Secuencia: **SU CR LF** 

Respuestas posibles: 

|**SU_A CR LF**|- comando entendido, comenzó a realizar|
|---|---|
|**SU_E CR LF**|- límite de tiempo superado en espera del resultado estable.|
|**SU_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|**SU_A CR LF**<br>**MARCO DE**<br>**MASA**|- comando entendido, comenzó a realizar<br>- valor de la masa se devuelve en la unidad básica|



Formato de marco de masa, que corresponde el indicador: 

|1<br>2<br>S<br>U|3<br>espacio|4<br>Signo<br>de<br>estabilidad|5<br>espacio|6<br>Signo|7-15<br>Masa|16<br>espacio|17|18<br>Unidad|19<br>|20<br>CR|21<br>LF|
|---|---|---|---|---|---|---|---|---|---|---|---|



8 

### **<u>Ejemplo:</u>** 

**S U CR LF** – orden del ordenador. **SU _ A CR LF** - comando entendido y comenzó a realizar. **S U _ _ _ - _ _ 1 7 2 .** 1 3 5 _ N _ _ CR LF - comando se terminó, valor de la masa se devuelve en la unidad actual usada. 

### **4.9. Poner el resultado inmediatamente en la unidad actual** 

Secuencia: **SUI CR LF** 

Respuestas posibles: 

|**SUI_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|---|---|
|**MARCO DE**|- el valor de la masa se devuelve en la unidad básica|
|**MASA**|inmediatamente.|



Formato de marco de masa, que corresponde el indicador: 

|1<br>2|3|4|5|6|7-15|16|17<br>18|19|20|21|
|---|---|---|---|---|---|---|---|---|---|---|
|S<br>U|I|Signo<br>de<br>estabilidad|espacio|Signo|Masa|espacio|Unidad||CR|LF|



### **<u>Ejemplo:</u>** 

**S U I CR LF** – orden del ordenador. 

**S U I ? _ - _ _ _ 5 8 . 2 3 7 _ k g _ CR LF** - comando se terminó, valor de la masa se devuelve en la unidad básica **Donde: _** - espacio 

### **4.10. Activar la transmisión continua en la unidad básica** 

Secuencia: **C1 CR LF** 

Respuestas posibles: 

|**C1_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|---|---|
|**C1_A CR LF**<br>**MARCO DE**<br>**MASA**|- comando entendido, comenzó a realizar<br>- valor de la masa se devuelve en la unidad básica|



Formato de marco de masa, que corresponde el indicador: 

|1<br>2|3|4|5|6|7-15|16|17<br>18|19|20|21|
|---|---|---|---|---|---|---|---|---|---|---|
|S<br>I|espacio|Signo<br>de<br>estabilidad|espacio|Signo|Masa|espacio|Unidad||CR|LF|



9 

### **4.11. Desconectar la transmisión continúa en la unidad básica** 

Secuencia: **C0 CR LF** 

Respuestas posibles: 

|**C0_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|---|---|
|**C0_A CR LF**|- comando entendidoyrealizado.|



### **4.12. Conectar la transmisión continúa en la unidad actual** 

Secuencia: **CU1 CR LF** 

Respuestas posibles: 

|**CU1_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|---|---|
|**CU1_A CR LF**<br>**MARCO DE**<br>**MASA**|- comando entendido, comenzó a realizar<br>- el valor de la masa se devuelve en la unidad actual.|



Formato de marco de masa, que corresponde el indicador: 

|1|2|3|4|5|6|7-15|16|17|18|19|20|21|
|---|---|---|---|---|---|---|---|---|---|---|---|---|
|S|U|I|Signo<br>de<br>estabilidad|espacio|Signo|Masa|espacio||Unidad||CR|LF|



### **4.13. Desconectar la transmisión continúa en la unidad actual** 

Secuencia: **CU0 CR LF** 

Respuestas posibles: 

|**CU0_I CR LF**|- comando entendido, pero en el momento no está disponible.|
|---|---|
|**CU0_A CR LF**|- comando entendidoyrealizado.|



### **4.14. Ajuste el umbral inferior de verificación de peso (controlador)** 

Secuencia: **DH_XXXXX CR LF** , donde **XXXXX** - formato de masa. 

Respuestas posibles: 

|**DH_OK CR LF**|- comando está hecho|
|---|---|
|**ES CR LF**|- comando no entendido(formato de masa incorrecto)|



10 

### **4.15. Ajuste el umbral superior de verificación de peso (controlador)** 

Secuencia: **UH_XXXXX CR LF** , donde **XXXXX** - formato de masa. 

Respuestas posibles: 

|**UH_OK CR LF**|- comando está hecho|
|---|---|
|**ES CR LF**|- comando no entendido(formato de masa incorrecto)|



### **4.16. Poner el valor de umbral inferior de verificación de peso (controlador)** 

Secuencia: **ODH CR LF** 

Respuesta: **DH_MASA CR LF** - comando realizado. 

Formato de respuestas: 

|1|2|3|4-12|13|14|15|16|17|18|19|
|---|---|---|---|---|---|---|---|---|---|---|
|D|H|espacio|Masa|espacio||Unidad||espacio|CR|LF|



**Masa** - 9 signos alineado a la derecha. **Unidad** - 3 signos alineado a la izquierda 

### **4.17. Poner el valor de umbral superior de verificación de peso (controlador)** 

Secuencia: **OUH CR LF** 

Respuesta: **UH_MASA CR LF** - comando realizado. 

Formato de marco de masa, que corresponde el indicador: 

|1|2|3|4-12|13|14|15|16|17|18|19|
|---|---|---|---|---|---|---|---|---|---|---|
|U|H|espacio|Masa|espacio||Unidad||espacio|CR|LF|



**Masa** - 9 signos alineado a la derecha. **Unidad** - 3 signos alineado a la izquierda 

### **4.18. Simulación de presionar el botón ENTER / PRINT** 

Secuencia: **SS CR LF** 

El envío del comando **S S CR LF** a la balanza da como resultado el registro automático del pesaje en la base de datos al mismo tiempo que se llama la impresión declarada. 



**_Al enviar un pedido a la balanza, se deben cumplir todas las condiciones de pesaje (control de resultados, lectura de peso estable, etc.)._** 

11 

### **4.19. Cambiar la unidad** 

Secuencia: **PN CR LF** , donde **N** - número de plataforma (de 1 a 4). 

Respuestas posibles: 

|**PN_OK CR LF**|- comando está hecho|
|---|---|
|**PN_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|**ES CR LF**|- comando no entendido (número deplataforma incorrecto)|



### **4.20. Introducir el número de fabrica** 

Secuencia: **NB CR LF** 

Respuestas posibles: 

|**NB_A_”Numero de serie”**<br>**CR LF**|- comando comprendido devuelve el número de serie.|
|---|---|
|**NB_I CR LF**|- comando entendido, pero en el momento no está<br>disponible.|



- **” número de serie** – Parámetro que define el número de serie del dispositivo. Devuelto entre comillas. 

### **<u>Ejemplo:</u>** 

**NB CR LF** – orden del ordenador. 

**NB_A_”123456” CR LF** – el número de serie del dispositivo– 123456. 

### **4.21. Ajustar la masa de pieza** 

Secuencia: **SM_XXXXX CR LF** , donde: _ -espacio XXXXX – formato de masa. 

Respuestas posibles: 

|**SM_OK CR LF**|- comando está hecho|
|---|---|
|**SM_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|**ES CR LF**|- comando no entendido(formato de masa incorrecto)|





**_El comando operado en el modo de trabajo "Conteo de piezas"._** 

### **4.22. Introducir la masa de referencia** 

Secuencia: **RM_XXXXX CR LF** , donde: _ -espacio XXXXX – formato de masa. 

Respuestas posibles: 

12 

|**RM_OK CR LF**|- comando está hecho|
|---|---|
|**RM_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|**ES CR LF**|- comando no entendido (formato de masa incorrecto)|





**_El comando operado en el modo de trabajo <Desviaciones>_** 

### **4.23. Activar la señal de sonido** 

Secuencia: **BP_TIME CR LF** , donde: _ - espacio, TIME - tiempo en [ms] de la señal de sonido. Rango recomendado <50÷5000>. 

Respuestas posibles: 

|**BP_OK CR LF**|- comando está hecho, ejecuta la señal BEEP|
|---|---|
|**BP_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|**ES CR LF**|- comando no entendido (formato de duración incorrecta)|



### **<u>Ejemplo:</u>** 

**BP_350 CR LF** - Comando de ordenador (activar el BEEP durante 350 ms de tiempo). **BP_OK CR LF** - BEEP activado. 





|**_Si se da un valor mayor que el límite alto permisible,_**<br>**_entonces el sonido BEEP se opera por el tiempo máximo_**<br>**_permisible._**|
|---|
|**_– BEEP llamado por comando BP, se interrumpe si durante_**<br>**_su período se activa la señal de otra fuente: teclado, panel_**<br>**_táctil, sensores de movimiento._**|



### **4.24. Introducir modos de trabajo disponibles** 

Secuencia: **OMI CR LF** 

Respuestas posibles: 

|**OMI CR LF**<br>**n_Nombre del modo**|- comando este hecho, devuelve los modos de|
|---|---|
|**CR LF**|funcionamiento disponibles|
|**OK CR LF**||
|**OMI_I CR LF**|- comando entendido,pero en el momento no está disponible.|



**Nombre del modo** - nombre del modo de trabajo, presentado como en la pantalla de un tipo de balanza dado, en el idioma de trabajo actualmente seleccionado en la balanza. 

13 

**n** – parámetro , valor decimal que especifica el número de modo de trabajo. 

### **Lista de modos de trabajo:** 

|1|PESAJE|
|---|---|
|2|Cálculo depiezas|
|3|DESVIACIONES|
|4|Dosificación|
|5|Recetas|
|6|Pesaje de animales|
|7|Densidad|
|8|Densidad de cuerpos sólidos|
|9|Densidad del liquido<br>|
|10|Cierre Máximo MÁX.|
|11|Suma|
|12|Controlador depeso(verificación)|
|13|Estadísticas|
|14|Calibración depipetas|
|15|Pesaje diferencial|
|16|Control estadístico de calidad(SQC)|
|17|Control de contenido envasado|
|18|Control de masa(pastillas)|
|19|Secado|
|20|Comparador|
|21|Básculas de coche|
||**_Numeración de modos de trabajo es asigno al nombre y_**<br>**_constante en todo tipo de balanza._**|



**<u>Ejemplo:</u> OMI CR LF** - orden del ordenador **OMI CR LF** - se devuelven los modos de trabajo disponibles **1_Pesaje CR LF 2_Calculo de piezas CR LF 3_Desviaciones CR LF OK CR LF** - el final de la ejecución del comando 

### **4.25. Ajustar modo de trabajo** 

Secuencia: **OMS_n CR LF** , donde: **n** – parámetro, el valor decimal que especifica el modo de trabajo. Descripción detallada, ver: orden **OMI** . 

14 

Respuestas posibles: 

|**OMS_OK CR LF**|- comando está hecho|
|---|---|
|**OMS_I CR LF**|- comando entendido,pero en el momento no está disponible.|
|**OMS_E CR LF**|- comando no entendido (valor no válido).|



### **<u>Ejemplo:</u>** 

**OMS_2 CR LF** - orden del ordenador. 

**OMS_OK CR LF** - seleccionado el modo de trabajo Contando piezas 

### **4.26. Introducir modo de trabajo actual** 

Secuencia: **OMG CR LF** 

Respuestas posibles: 

|**OMG_n_Nazwa modu CR LF**|– comando realizado, devuelve el número del modo<br>de trabajo actual.|
|---|---|
|**OMG_I CR LF**|- comando entendido, pero en el momento no está<br>disponible.|



**Nombre del modo** - nombre del modo de trabajo, presentado como en la pantalla de un tipo de balanza dado, en el idioma de trabajo actualmente seleccionado en la balanza. 

**n** – parámetro , valor decimal que especifica el número de modo de trabajo. Descripción detallada, ver: orden **OMI** . 

### **<u>Ejemplo:</u>** 

**OMG CR LF** - orden del ordenador. 

**OMG_2_Liczenie sztuk CR LF** - dispositivo en el modo Calculo de piezas. 

### **4.27. Enviar todos los comandos aplicados** 

Secuencia: **PC CR LF** 

Respuesta: 

**PC_A_"Z,T,S,SI,SU,SUI,C1,C0,CU1,CU0,DH,ODH,UH,OUH,OT,UT,SIA,SS, PC,P1,P2,P3,P4,NB,SM,RM,BP,OMI,OMS,OMG"** - comando realizado, el indicador muestra todos los comando implementados. 

15 

## **5.  Impresión manual/ Impresión automática** 

El usuario puede generar de balanza las impresiones manuales o automáticas: 

- La impresión manual se genera después de colocar la carga pesada en la plataforma de pesaje y después de que el resultado de la pesada se haya estabilizado presionando la tecla ENTER / PRINT. 

- La impresión automática se genera automáticamente después de colocar la carga pesada en la plataforma de pesaje y la estabilización del resultado de pesaje. 

### **Formato de impresión:** 

|1|2|3|4 -12|13|14|15<br>16|17|18|
|---|---|---|---|---|---|---|---|---|
|Signo<br>de<br>estabilidad|espacio|Signo|Masa|espacio||Unidad|CR|LF|
|**Signo de est**|**abilidad**|[espacio<br>[?]si es i<br>[^]si hay<br>[v]si ha|] si es esta<br>nestable<br>un error q<br>yun error|ble<br>ue superad<br>que supera|o el ra<br>do el r|ngo de +<br>ango de -|||
|**Signo**||[espacio|] para valo|respositiv|os o[-]|para valores neg|ativos||
|**Masa**||9 signos|con elpu|nto alinead|o a la d|erecha|||
|**Unidad**||3 signos|alineado|a la izquier|da||||



### **<u>Ejemplo:</u>** 

- **_ _ _ _ _ _ 1 8 3 2 . 0 _ g _ _ CR LF** - impresión generada por la balanza después de pulsar **ENTER/PRINT** . 

16 

17 

18 

