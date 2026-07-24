CloudSystem

1. Objetivo

O CloudSystem é responsável por controlar toda a representação visual das nuvens do jogo.

Ele não decide o clima nem executa lógica meteorológica. Sua única responsabilidade é interpretar os parâmetros recebidos do WeatherSystem, gerar pequenas variações naturais e aplicá-las ao sistema de nuvens do HDRP e às nuvens de fundo.

O sistema deve produzir transições suaves e contínuas, evitando mudanças abruptas e mantendo aparência orgânica durante longos períodos de jogo.

2. Responsabilidades

O CloudSystem deve ser responsável por:

    controlar as Volumetric Clouds do HDRP;
    controlar as Background Clouds;
    gerar pequenas variações naturais para os valores definidos pelo WeatherSystem;
    interpolar continuamente todos os parâmetros;
    sincronizar os dois sistemas de nuvens;
    fornecer uma API pública simples para consulta do estado atual.

Não é responsabilidade do CloudSystem:

    decidir qual clima está ativo;
    iniciar chuva;
    controlar vento;
    controlar iluminação;
    controlar horário.

3. Dependências

Entrada

Recebe informações exclusivamente do:

WeatherSystem

Opcionalmente poderá ler:

WindSystem

Somente se futuramente for confirmado que o HDRP não movimenta automaticamente as Volumetric Clouds através do Wind Volume.

4. Arquitetura

                WeatherSystem
                      │
                      ▼
               CloudSystem
                      │
     ┌────────────────┼────────────────┐
     │                │                │
     ▼                ▼                ▼
CloudState      Transition      HDRP Adapter
Generator       Controller
                                      │
                                      ▼
                            Volumetric Clouds

                      │
                      ▼
           Background Cloud Adapter
                      │
                      ▼
              Background Clouds

5. Componentes Internos
5.1 Cloud State Generator

Responsável por converter os valores recebidos do WeatherSystem em um estado alvo de nuvens.

Responsabilidades:

    receber valores base do WeatherProfile;
    aplicar modificadores de variação natural através do CloudNoise;
    gerar novos estados alvo;
    respeitar limites internos definidos pelo CloudSystem;
    não aplicar valores diretamente no HDRP.

O Cloud State Generator não utiliza valores aleatórios puros. A evolução das nuvens deve ser contínua e previsível.

Exemplo:

WeatherProfile:

CloudCoverage = 0.50


CloudSystem:

Noise Variation = ±0.08


Estados gerados:

0.50
0.53
0.56
0.58
0.55
0.51
0.47

5.2 CloudNoise

Responsável por simular pequenas variações atmosféricas naturais nas propriedades das nuvens.

O objetivo do CloudNoise é evitar que as nuvens permaneçam visualmente estáticas, criando uma evolução contínua semelhante aos processos atmosféricos reais.

Ele utiliza funções de ruído contínuo, como:

    Perlin Noise;
    Simplex Noise;
    Fractal Brownian Motion (FBM), se necessário.

Não deve utilizar valores aleatórios independentes, pois isso causaria mudanças bruscas e artificiais.
Funcionamento

Cada propriedade controlada possui sua própria configuração de ruído.

Exemplo:

CloudCoverage

Amplitude:
0.05

Frequency:
0.1

Speed:
0.02

O valor final é calculado:

Valor Final =
Valor Base do WeatherProfile
+
CloudNoise(valor)

Exemplo:

Coverage Base:

0.60


CloudNoise:

+0.03


Resultado:

0.63

5.2.1 Parâmetros do CloudNoise

Cada parâmetro pode possuir uma configuração independente.

Exemplo:
Coverage Noise

Controla:

    aumento/diminuição gradual da quantidade de nuvens.

Configuração:

Amplitude baixa
Frequência baixa
Mudança lenta

Density Noise

Controla:

    variações internas de densidade.

Configuração:

Amplitude média
Frequência média

Color Noise

Controla:

    pequenas variações de tonalidade.

Configuração:

Amplitude muito baixa
Frequência muito baixa

Evita que a iluminação das nuvens pareça pulsar.
5.2.2 Regras do CloudNoise

O CloudNoise deve:

    produzir valores contínuos;
    evitar mudanças abruptas;
    respeitar limites máximos e mínimos internos;
    funcionar independentemente do clima atual;
    permitir configuração por propriedade.

O CloudNoise não deve:

    decidir o clima;
    criar tempestades;
    alterar iluminação;
    controlar vento.

5.3 Transition Controller

Responsável por interpolar o estado atual até o novo estado alvo.

O estado recebido já contém os efeitos do CloudNoise.

Fluxo:

WeatherProfile

↓

Cloud State Generator

↓

CloudNoise

↓

Target Cloud State

↓

Transition Controller

↓

HDRP Adapter


5.4 Background Cloud Adapter

Responsável por controlar nuvens extremamente distantes.

Essas nuvens não influenciam:

    iluminação;
    sombras;
    clima;
    chuva.

Sua única função é aumentar a sensação de profundidade.

6. Background Clouds

As nuvens de fundo serão implementadas utilizando meshes posicionados a grande distância (domo, anel ou geometria equivalente) com materiais específicos para nuvens.

Responsabilidades:

    preencher o horizonte;
    representar massas de nuvens além do alcance das volumétricas;
    acompanhar visualmente o estado das nuvens volumétricas.

Devem possuir controle independente de:

    cobertura;
    cor;
    brilho;
    opacidade;
    velocidade de deslocamento (caso não seja derivada automaticamente do vento).

7. Comunicação com WeatherSystem

O WeatherSystem fornece, via perfil climático, fornece numeros alvo para todas as propriedades relevantes das nuvens. 

Exemplo simplificado:

Sunny

Coverage
0.10 

Density
0.20 

Cloud Color
White

Erosion
0.60

Storm

Coverage
0.90

Density
0.80

Cloud Color
Gray -> Dark Gray

Erosion
0.20

O CloudSystem utiliza um desvio para produzir pequenas variações naturais em cima dos numeros alvo.

8. Filosofia das Variações

O céu nunca deve permanecer completamente estático.

Mesmo durante um dia ensolarado deve existir variação contínua de baixa intensidade.

Essas variações simulam:

    formação de nuvens;
    dissipação;
    alterações de iluminação;
    pequenas mudanças atmosféricas.


9. Parâmetros Controlados

A lista deverá acompanhar os recursos disponíveis no HDRP Volumetric Clouds.

Inicialmente o sistema deverá suportar:

    Coverage
    Density
    Cloud Color
    Ambient Exposure
    Erosion
    Shape Factor
    Density Multiplier
    Powder Effect
    Multi Scattering
    Cloud Opacity
    Cloud Bottom Tint
    Cloud Top Tint
    Altitude (quando aplicável)
    Thickness (quando aplicável)

A lista poderá ser expandida conforme novas versões do HDRP.

10. Estados Climáticos Esperados

Exemplos:
Céu Limpo

    poucas nuvens
    alta erosão
    cor branca
    baixa densidade

Parcialmente Nublado

    cobertura média
    nuvens volumosas
    pequenas variações de iluminação

Nublado

    cobertura elevada
    menor incidência de luz direta
    densidade alta

Tempestade

    cobertura máxima
    coloração escura
    densidade muito alta
    alterações mais frequentes

11. Atualização

Fluxo de execução:

WeatherSystem

↓

CloudSystem

↓

State Generator

↓

Transition Controller

↓

HDRP Adapter

↓

Volume HDRP

As Background Clouds devem receber o mesmo estado interpolado.

12. Desempenho

Objetivos:

    evitar alocações em tempo de execução;
    evitar criação/destruição de objetos;
    reutilizar estruturas internas;
    executar atualização apenas quando necessário.

13. Extensibilidade

O sistema deve permitir futura integração com:

    RainSystem
    LightningSystem
    FogSystem os
    TimeOfDay
    Seasons
    Save/Load
    Multiplayer

Sem necessidade de alterar sua arquitetura principal.

14. Princípios de Projeto (atualizado)

O CloudSystem deve seguir:

    responsabilidade única;
    separação entre dados e comportamento;
    ausência de aleatoriedade brusca;
    variações naturais através de ruído contínuo;
    interpolação de todos os valores;
    baixo acoplamento entre sistemas;
    possibilidade de reprodução através de seed configurável.

Essa alteração deixa o sistema mais próximo de uma simulação atmosférica: o WeatherSystem define o "clima médio", e o CloudSystem cria a dinâmica visual em torno desse estado.


15. Objetivo Final

Criar um sistema de nuvens modular, previsível e facilmente expansível, onde:

    o WeatherSystem define como o clima deve ser;
    o CloudSystem decide como representar visualmente esse clima, adicionando pequenas variações naturais;
    o HDRP atua apenas como camada de renderização, sem conter lógica de negócio.

Essa separação mantém cada sistema com uma responsabilidade clara e facilita a evolução futura do sistema climático sem introduzir dependências desnecessárias.
