# Documento de Arquitetura Técnica

## Projeto – Simulador Contemplativo de Navegação (Unity 6.5)

### Visão Geral

O objetivo do projeto é criar um jogo focado em navegação à vela, exploração marítima e contemplação.

Não existem combates ou mecânicas de sobrevivência tradicionais. O desafio do jogador é interpretar o ambiente e dominar a navegação através de fatores naturais como vento, clima, correntes marítimas, recifes e mar revolto.

O oceano é o cenário principal. As ilhas funcionam apenas como pontos de parada, referência visual e locais de interesse, não havendo exploração terrestre.

Como consequência, o projeto prioriza sistemas relacionados ao mar e reduz investimentos em tecnologias voltadas para terrenos extensos ou vegetação complexa.

---

# Sistemas de terceiros

## Oceano

**Crest Ocean System**

Responsável por:

* simulação das ondas
* interação física do barco
* espuma
* reflexão
* refração
* ondulação dinâmica
* interação entre vento e mar

Será o sistema central do projeto.

---

## Clima

### Opção atual

**UniStorm**

Atualmente é o candidato mais forte.

Motivos:

* ciclo dia/noite
* chuva
* tempestades
* neblina
* vento
* nuvens
* iluminação integrada
* bom desempenho
* menor custo que soluções mais completas

O sistema deverá controlar diretamente diversos aspectos do jogo:

* intensidade do vento
* direção do vento
* visibilidade
* estado do mar
* chuva
* neblina
* iluminação

O Crest deverá responder às variáveis climáticas fornecidas pelo UniStorm.

---

## Câmera

**Unity Cinemachine**

Utilizado para:

* câmera principal
* transições suaves
* diferentes modos de navegação
* câmera contemplativa
* câmera livre

---

## Save System

**Easy Save 3**

Responsável por salvar:

* posição do barco
* clima atual
* progresso
* descobertas
* diário
* mapa
* rotas registradas

---

## Áudio

**FMOD** (preferencialmente)

O áudio terá papel fundamental na atmosfera do jogo.

O sistema deverá permitir:

* vento dinâmico
* intensidade do mar
* cordas
* velas
* casco
* chuva
* trovões
* gaivotas
* baleias
* golfinhos

Além disso, permitirá música adaptativa conforme o clima e o estado da navegação.

---

# Sistemas próprios

## Sistema de Navegação

Este é o principal diferencial do jogo.

Será desenvolvido especificamente para o projeto.

Responsável por:

* comportamento das velas
* física simplificada da navegação
* resposta ao vento
* inércia
* aceleração
* manobras
* influência das ondas
* influência das correntes

---

## Weather Manager

Mesmo utilizando UniStorm, será criado um controlador próprio para centralizar os dados climáticos.

Exemplo de variáveis:

* Wind Direction
* Wind Speed
* Sea State
* Rain Amount
* Fog Density
* Visibility

Todos os demais sistemas consultarão esse gerenciador.

Isso evita dependência direta entre gameplay e o asset de clima.

---

## Correntes Marítimas

Sistema próprio.

Cada região do oceano poderá possuir:

* direção
* intensidade
* influência sobre a embarcação

As correntes deverão alterar naturalmente a navegação, incentivando o jogador a interpretar o ambiente.

---

## Sistema de Recifes e Perigos

O oceano conterá diversos perigos naturais.

Exemplos:

* recifes
* bancos de areia
* pedras submersas
* águas rasas

Esses elementos deverão ser percebidos através da observação do ambiente, da cor da água, do relevo submarino e da experiência do jogador.

---

## Sistema de Descobertas

Locais importantes serão registrados conforme encontrados.

Exemplos:

* ilhas
* enseadas
* faróis
* portos
* pontos seguros
* recifes
* áreas perigosas

O objetivo é criar uma sensação de construção gradual do conhecimento sobre o mundo.

---

## Diário de Bordo

Sistema desenvolvido especificamente para o jogo.

Funcionará como registro permanente da viagem.

Possíveis informações:

* data
* clima
* distância percorrida
* observações
* locais visitados
* eventos importantes
* notas do jogador

O diário poderá ser parcialmente automático e parcialmente editável pelo jogador.

---

## Carta Náutica

Um dos sistemas centrais do projeto.

Não será um minimapa convencional.

A proposta é utilizar uma carta náutica semelhante às utilizadas na navegação real.

Funcionalidades previstas:

* marcação manual de perigos
* desenho de rotas
* marcação de ancoradouros
* marcação de recifes
* anotações livres
* registro de descobertas
* marcação de pontos de interesse
* possibilidade de utilizar símbolos náuticos

Esse sistema faz parte da identidade do jogo e deve ser desenvolvido especificamente para ele.

Caso seja utilizado algum asset, ele servirá apenas como base de interface (zoom, pan e renderização), enquanto toda a lógica permanecerá própria.

---

# Ilhas

As ilhas terão função principalmente visual e de navegação.

Não haverá exploração terrestre.

Portanto, não serão necessários sistemas avançados de:

* vegetação procedural
* geração de terreno
* fauna terrestre
* exploração em primeira pessoa

O foco será criar costas visualmente interessantes que auxiliem na orientação do jogador.

---

# Escopo de Compra de Assets

## Comprar

* Crest Ocean System
* UniStorm
* Easy Save 3
* Cinemachine
* FMOD (ou outro middleware de áudio)
* Bibliotecas de sons marítimos
* Modelos de barcos secundários
* Modelos de faróis
* Rochas costeiras
* Corais
* Boias
* Elementos portuários
* Fauna marinha e aves

---

## Desenvolver Internamente

* Física do veleiro
* Sistema de navegação
* Weather Manager
* Correntes marítimas
* Carta náutica
* Diário de bordo
* Sistema de descobertas
* Registro de perigos
* Sistema de exploração marítima
* Progressão do jogador

---

# Filosofia do Projeto

Sempre que possível, utilizar assets para resolver problemas técnicos complexos já consolidados (água, clima, câmera, salvamento e áudio).

Todo o tempo de desenvolvimento deve ser concentrado naquilo que torna o jogo único: a experiência de navegar, interpretar o ambiente, registrar descobertas e construir conhecimento sobre o oceano ao longo da viagem.

# Sistema de Instrumentos de Navegação

Os instrumentos de navegação terão papel central na experiência do jogador.

Em vez de fornecer informações por meio de HUDs tradicionais, o jogo privilegiará instrumentos físicos presentes na embarcação, incentivando a observação e a interpretação do ambiente.

A intenção é reforçar a imersão e transmitir a sensação de realmente comandar um veleiro.

---

## Objetivos

* reduzir a dependência de elementos de interface;
* incentivar a navegação por observação;
* criar uma experiência contemplativa e imersiva;
* transformar o aprendizado da navegação em parte da progressão do jogador.

---

## Instrumentos Básicos

### Bússola

Principal referência de direção.

Utilizada para:

* manter o rumo;
* seguir rotas planejadas;
* orientar a leitura da carta náutica.

---

### Relógio

Permite acompanhar a passagem do tempo.

Utilizado em conjunto com:

* ciclo de dia e noite;
* planejamento de viagens;
* previsão aproximada do pôr e nascer do sol;
* registro automático no diário de bordo.

---

### Indicador de Velocidade da Embarcação (Log)

Apresenta a velocidade atual do veleiro.

Pode ser utilizado para:

* estimar tempo de viagem;
* avaliar a eficiência do ajuste das velas;
* auxiliar na navegação estimada (Dead Reckoning).

---

### Indicador de Direção e Intensidade do Vento

Exibe a direção do vento em relação ao barco e sua intensidade.

Essas informações são essenciais para:

* ajustar as velas;
* escolher o melhor rumo;
* antecipar mudanças climáticas;
* otimizar a velocidade da embarcação.

---

### Profundímetro

Indica a profundidade abaixo da embarcação.

Utilizado para:

* evitar encalhes;
* identificar bancos de areia;
* navegar com segurança em áreas costeiras;
* aproximar-se de portos e enseadas.

---

## Instrumentos Avançados

Estes instrumentos podem ser desbloqueados ao longo da progressão do jogo ou estar disponíveis em modos de navegação mais realistas.

### Sextante

Permite determinar a posição aproximada utilizando a altura dos astros.

Seu uso pode ser simplificado para manter a acessibilidade sem perder a autenticidade.

---

### Barômetro

Indica mudanças na pressão atmosférica.

Pode servir como ferramenta para prever tempestades e alterações no clima antes que sejam visualmente perceptíveis.

---

### Termômetro

Apresenta a temperatura ambiente.

Embora tenha pouca influência direta na jogabilidade, contribui para a ambientação e pode complementar previsões meteorológicas.

---

### Anemômetro

Fornece uma leitura precisa da velocidade do vento.

Pode complementar a observação visual das ondas, bandeiras e velas.

---

## Integração com Outros Sistemas

Os instrumentos deverão utilizar informações fornecidas pelo **Weather Manager**, garantindo consistência entre:

* clima;
* vento;
* estado do mar;
* navegação;
* carta náutica;
* diário de bordo.

Mudanças nas condições ambientais deverão refletir imediatamente nas leituras dos instrumentos.

---

## Filosofia de Design

Sempre que possível, o jogador deverá obter informações observando os instrumentos da embarcação, e não através de indicadores permanentes na tela.

A interface tradicional deverá ser mínima, permitindo que a cabine e o convés funcionem como a principal fonte de informações para a navegação.

Essa abordagem reforça a proposta contemplativa do jogo e valoriza a sensação de comandar um veleiro em um ambiente natural.

# Adendo – Progressão, Tripulação e Gestão de Risco

## Filosofia de Progressão

A progressão do jogador será baseada na evolução de uma única embarcação ao longo de toda a campanha.

O objetivo é criar um vínculo entre jogador e barco, fazendo com que cada melhoria represente uma etapa da jornada, em vez de substituir constantemente a embarcação por modelos superiores.

As melhorias poderão incluir:

* casco;
* velas;
* mastros;
* cabine;
* compartimentos de carga;
* instrumentos de navegação;
* iluminação;
* elementos estéticos.

---

## Comércio e Motivação para Exploração

O jogo poderá contar com um sistema de comércio simplificado, cujo objetivo não será simular uma economia complexa, mas gerar motivos para navegar.

As viagens poderão envolver:

* transporte de mercadorias;
* entrega de suprimentos;
* transporte de passageiros;
* expedições científicas;
* entregas urgentes;
* encomendas especiais.

O foco está na viagem e nas decisões tomadas durante ela, e não na compra e venda de produtos.

As recompensas obtidas financiarão melhorias da embarcação, contratação de tripulantes e aquisição de novos equipamentos.

---

## Gestão de Risco

Durante uma viagem poderão surgir eventos climáticos capazes de alterar o planejamento do jogador.

As tempestades não deverão funcionar como eventos obrigatórios nem como desafios isolados, mas como decisões estratégicas de risco e recompensa.

Exemplos:

* contornar uma tempestade aumenta significativamente o tempo de viagem;
* mercadorias perecíveis podem estragar devido ao atraso;
* passageiros podem ficar insatisfeitos com a demora;
* contratos podem sofrer redução na recompensa;
* determinadas oportunidades podem ser perdidas.

Por outro lado, atravessar a tempestade preserva o cronograma original da viagem, mantendo a carga, os contratos e os compromissos, porém expondo a embarcação e a tripulação a riscos muito maiores.

O jogador deverá avaliar constantemente se vale a pena assumir o perigo ou optar por uma rota mais segura.

Não existe uma resposta correta; a decisão dependerá da situação da embarcação, da experiência da tripulação, das condições climáticas e da importância da missão em andamento.

---

## Sistema de Danos

O objetivo não é punir o jogador com um "Game Over", mas criar consequências permanentes para decisões arriscadas.

A embarcação poderá sofrer danos como:

* quebra do leme;
* danos ao casco;
* perda de velas;
* quebra de mastros;
* infiltração de água;
* danos aos instrumentos de navegação;
* perda parcial da carga.

Após sofrer danos significativos, a prioridade do jogador passa a ser alcançar um porto seguro para realizar reparos.

A navegação com equipamentos danificados deverá alterar a forma de jogar, tornando a recuperação parte natural da experiência.

---

## Tripulação

O jogador assume o papel de capitão, emitindo ordens em vez de executar diretamente cada tarefa da embarcação.

Exemplos de comandos:

* içar ou recolher velas;
* preparar a âncora;
* sondar a profundidade;
* realizar pequenos reparos;
* ajustar o rumo.

A tripulação será responsável por executar essas ações.

---

## Personalidade e Especialização da Tripulação

Cada tripulante possuirá características próprias que influenciarão a eficiência da embarcação.

Exemplos de atributos:

* velocidade para operar velas;
* habilidade de navegação;
* experiência em tempestades;
* capacidade de realizar reparos;
* percepção para identificar mudanças climáticas;
* resistência física;
* calma sob pressão.

O objetivo não é criar um RPG complexo, mas fazer com que cada integrante tenha identidade e gere decisões interessantes na composição da equipe.

---

## Evolução da Tripulação

Ao longo da campanha, o jogador poderá contratar novos tripulantes em diferentes portos.

Cada porto poderá oferecer profissionais com perfis distintos, incentivando a busca por especialistas que complementem as necessidades da embarcação.

Da mesma forma, acidentes e decisões tomadas durante as viagens poderão resultar na perda de membros da tripulação, aumentando o peso das escolhas e reforçando a sensação de responsabilidade do capitão.

# Adendo – Escala do Mundo, Ritmo da Navegação e Gestão do Tempo

## Filosofia de Escala

O mundo será inspirado em uma região real do planeta, porém não será uma reprodução fiel.

A geografia deverá ser inspirada em arquipélagos tropicais, preservando as características naturais da navegação marítima (ilhas, estreitos, recifes, baías e mar aberto), mas com liberdade para adaptar dimensões e posicionamentos em favor da jogabilidade.

As distâncias serão reduzidas em relação às escalas reais, mantendo a sensação de grandes travessias sem exigir tempos excessivos de deslocamento.

O objetivo é transmitir a percepção de um oceano vasto, sem comprometer o ritmo da experiência.

---

## Duração das Viagens

As viagens deverão possuir diferentes escalas de duração, de acordo com sua importância.

### Travessias Curtas

Tempo estimado:

* 5 a 8 minutos.

Utilizadas para deslocamentos locais entre ilhas próximas.

---

### Travessias Médias

Tempo estimado:

* 10 a 15 minutos.

Representam o núcleo da experiência de navegação.

Esse intervalo oferece tempo suficiente para:

* observar o ambiente;
* ajustar velas;
* consultar instrumentos;
* interpretar mudanças climáticas;
* planejar a rota.

---

### Grandes Expedições

Tempo estimado:

* 20 a 30 minutos.

Reservadas para viagens especiais entre regiões mais distantes.

Essas viagens deverão exigir planejamento prévio e preparação da embarcação.

---

## Filosofia de Ritmo

O desafio da navegação não deverá ser baseado na quantidade de eventos, mas na necessidade constante de observar e interpretar o ambiente.

Durante qualquer viagem, o jogador deverá estar atento a elementos como:

* mudanças na direção do vento;
* formação de tempestades no horizonte;
* alteração da cor da água indicando áreas rasas;
* aproximação de recifes;
* redução da profundidade;
* presença de embarcações;
* aves indicando proximidade de terra;
* mudanças nas correntes marítimas.

Mesmo quando nenhuma situação crítica estiver ocorrendo, o ambiente deverá transmitir informações úteis para a tomada de decisão.

O princípio de design será:

> O jogador nunca deve permanecer longos períodos sem ter algo relevante para observar ou interpretar.

---

## Aceleração do Tempo

Para preservar a sensação de grandes distâncias sem tornar a navegação repetitiva, o jogo poderá oferecer um sistema de aceleração do tempo.

A aceleração somente estará disponível quando a embarcação estiver em condições seguras de navegação, como:

* mar aberto;
* vento estável;
* ausência de obstáculos próximos;
* ausência de tempestades iminentes;
* nenhuma ação imediata requerida da tripulação.

Durante esse período, considera-se que a tripulação mantém as operações rotineiras da embarcação enquanto o capitão acompanha a viagem.

A velocidade retornará automaticamente ao tempo normal sempre que ocorrer um evento relevante, como:

* mudança significativa do vento;
* aproximação de tempestades;
* terra à vista;
* aproximação de recifes;
* redução da profundidade;
* encontro com outras embarcações;
* solicitações da tripulação;
* qualquer situação que exija uma decisão do jogador.

Esse sistema busca preservar a escala do oceano sem transformar longas travessias em períodos de espera passiva.

---

## Princípio Fundamental

A duração de uma viagem não será determinada apenas pela distância percorrida, mas pela quantidade e pela qualidade das decisões tomadas durante o percurso.

Uma boa travessia é aquela em que o jogador precisou observar, interpretar o ambiente e tomar decisões de navegação, independentemente da distância física percorrida.


# **Geração do Mundo e Divisão entre Conteúdo Planejado e Procedural**

## **Visão Geral**

O mundo do jogo será dividido em chunks de 5 km x 5 km. O ambiente completo será gerado no início da partida e permanecerá estático durante o jogo.

Apenas 9 chunks permanecerão carregados simultaneamente:

* Chunk atual do jogador.  
* 8 chunks adjacentes ao redor.

O jogo ocorre integralmente no barco, sem exploração terrestre. Portanto, a prioridade da geração do mundo é criar um ambiente marítimo interessante, com boa leitura de navegação, rotas estratégicas e desafios físicos.

---

# **Filosofia de Geração**

O mundo será construído utilizando uma combinação de:

* **Conteúdo planejado manualmente:** elementos que influenciam gameplay, navegação e identidade do mundo.  
* **Conteúdo procedural:** elementos de variação, preenchimento e detalhamento visual.

A geração procedural não deve substituir o design do mundo. Ela deve ampliar e variar uma estrutura previamente definida.

---

# **Elementos Planejados Manualmente**

## **Geografia Principal**

A estrutura macro do mundo será definida manualmente.

Inclui:

* Localização das principais ilhas.  
* Formato geral dos arquipélagos.  
* Canais de navegação.  
* Regiões perigosas.  
* Áreas de interesse.  
* Rotas marítimas.  
* Distribuição dos biomas.

Motivo:

A navegação depende de referências espaciais claras. Um mundo totalmente procedural tende a gerar ambientes sem intenção de design e com menor valor estratégico.

---

## **Portos**

Os portos serão posicionados manualmente.

Cada porto terá definido:

* Localização.  
* Tamanho.  
* Importância econômica.  
* Facilidade ou dificuldade de acesso.  
* Características da região.

A geração procedural será utilizada apenas para variações visuais.

Exemplos:

* Quantidade de embarcações atracadas.  
* Distribuição de caixas e objetos.  
* Pequenas construções.  
* Elementos decorativos.

O layout principal dos portos deve permanecer planejado para garantir boa experiência de navegação.

---

## **Grandes Áreas de Perigo**

Elementos que alteram decisões de navegação serão definidos manualmente.

Exemplos:

* Grandes campos de coral.  
* Regiões com muitas rochas.  
* Estreitos perigosos.  
* Áreas de pouca profundidade.  
* Zonas de navegação restrita.

Esses elementos devem existir por intenção de gameplay, não apenas por aleatoriedade.

---

# **Elementos Procedurais**

## **Detalhamento das Ilhas**

A partir de uma definição inicial de ilha, o sistema procedural será responsável por gerar variações locais.

Inclui:

* Irregularidade da costa.  
* Pequenas enseadas.  
* Praias.  
* Falésias.  
* Pequenas formações rochosas.  
* Detalhes costeiros.

Exemplo:

Ilha planejada:

* Localização.  
* Tamanho.  
* Bioma.

Procedural:

* Formato final da costa.  
* Distribuição de detalhes.  
* Elementos visuais.

---

## **Fundo do Oceano**

O relevo submarino será gerado proceduralmente.

Inclui:

* Variação de profundidade.  
* Elevações submarinas.  
* Depressões.  
* Bancos de areia.  
* Áreas de coral.  
* Formações rochosas.

Possíveis métodos:

* Noise functions (Perlin/Simplex).  
* Máscaras de profundidade.  
* Regras específicas por bioma.

Exemplo:

Zona rasa:

* Areia.  
* Corais.  
* Rochas.

Zona intermediária:

* Relevo irregular.  
* Vegetação submarina.

Zona profunda:

* Pouca variação.  
* Oceano aberto.

---

## **Perigos Físicos Locais**

Os perigos físicos terão uma abordagem híbrida.

Definição manual:

* Localização de áreas perigosas.  
* Intensidade.  
* Importância para gameplay.

Geração procedural:

* Distribuição dos objetos dentro dessas áreas.  
* Variação de densidade.  
* Posicionamento individual.

Exemplo:

Área de coral definida no mapa:

* Região de 2 km².

Procedural:

* Quantidade de formações.  
* Posição dos corais.  
* Pequenas rochas associadas.

---

# **Sistema de Seeds e Geração**

Cada elemento procedural utilizará uma seed baseada no mundo e no chunk.

Estrutura:

World Seed  
    ↓  
Chunk Seed  
    ↓  
Geradores locais  
    ↓  
Elementos procedurais

Exemplo:

WorldSeed: 12345

Chunk (2,3)

Seed:  
Hash(WorldSeed \+ Coordenada do Chunk)

Resultado:  
\- Rochas  
\- Corais  
\- Detalhes costeiros  
\- Objetos ambientais

Isso garante:

* Reprodutibilidade.  
* Consistência entre carregamentos.  
* Facilidade de testes.  
* Possibilidade de salvar apenas dados necessários.

---

# **Fluxo de Geração Inicial**

1. Criar mapa mestre do mundo.  
2. Definir ilhas, portos e áreas importantes.  
3. Dividir o mapa em chunks.  
4. Aplicar regras de geração procedural.  
5. Gerar detalhes ambientais.  
6. Salvar o resultado final.

O mundo gerado não será alterado durante a partida.

---

# **Escopo da Prova de Conceito**

O primeiro protótipo será um ambiente de:

* 5 x 5 chunks.  
* Assets gratuitos.  
* Navegação marítima.  
* Mundo estático.

Distribuição inicial sugerida:

Conteúdo planejado:

* 3 a 5 ilhas principais.  
* 1 ou 2 portos.  
* Rotas navegáveis.  
* Áreas perigosas principais.

Conteúdo procedural:

* Costas.  
* Fundo oceânico.  
* Rochas.  
* Corais.  
* Bancos de areia.  
* Elementos ambientais.

---

# **Princípio Geral**

A geração procedural deve ser utilizada para aumentar variedade e reduzir trabalho manual, enquanto os elementos que influenciam decisões do jogador devem permanecer sob controle do design.

O objetivo é criar um mundo marítimo consistente, navegável e estratégico, evitando um ambiente aleatório sem propósito.

# **Sistemas Essenciais de Navegação \- Prova de Conceito**

## **1\. Sistema de Vento Dinâmico**

### **Objetivo**

Criar um sistema de navegação baseado na interpretação das condições de vento, fazendo com que o jogador precise ajustar sua estratégia, controlar as velas e escolher rotas adequadas.

O vento será um dos principais elementos de gameplay, influenciando diretamente a velocidade e a capacidade de deslocamento do veleiro.

---

## **Funcionamento Inicial**

O vento será representado por um vetor global contendo:

* Direção do vento.  
* Intensidade do vento.

Representação técnica inicial:

Wind Vector:  
\- Direction  
\- Strength

A força aplicada ao barco será calculada considerando:

* Direção do vento em relação ao barco.  
* Orientação das velas.  
* Eficiência da configuração das velas.  
* Resistência da água.

Modelo simplificado:

Força aplicada \=  
Vento × Eficiência da vela × Orientação da vela

---

## **Feedback ao Jogador**

Durante a prova de conceito, como serão utilizados assets prontos e não haverá sistema visual avançado de interação com velas, a indicação do vento será feita inicialmente por HUD.

Informações apresentadas:

* Direção do vento.  
* Intensidade aproximada.  
* Relação entre vento e direção atual do barco.

A implementação final poderá substituir ou complementar essa indicação através de elementos visuais do próprio navio:

* Comportamento das velas.  
* Flâmula no mastro.  
* Movimento de cordas.  
* Reação da tripulação.

---

# **2\. Sistema de Velas**

## **Objetivo**

Transformar o vento em uma mecânica ativa de controle do jogador.

As velas serão responsáveis por converter a força do vento em movimento, exigindo ajustes constantes para maximizar eficiência.

---

## **Mecânicas Básicas**

O sistema inicial deve permitir:

* Abrir e recolher velas.  
* Ajustar posição das velas.  
* Controlar eficiência de acordo com a direção do vento.

O jogador deverá entender que:

Vento favorável:  
\+ velocidade

Vento lateral:  
\+ velocidade  
\+ necessidade de ajuste

Contra o vento:  
\- velocidade  
\- necessidade de mudança de rota

---

## **Implementação Inicial**

Para a prova de conceito:

* A física das velas pode ser simplificada.  
* O foco é validar a tomada de decisão do jogador.  
* A representação visual pode ser substituída por indicadores temporários.

---

# **3\. Sistema de Correntes Marítimas**

## **Objetivo**

Adicionar outro elemento estratégico à navegação, fazendo com que o jogador considere não apenas o vento, mas também o movimento natural da água.

---

## **Funcionamento Inicial**

A corrente será representada por:

* Direção.  
* Intensidade.

Modelo:

Current Vector:  
\- Direction  
\- Strength

A corrente afetará:

* Velocidade real do barco.  
* Trajetória.  
* Aproximação de portos.  
* Passagem por regiões estreitas.

---

## **Implementação Inicial**

Durante a prova de conceito, as correntes podem ser aplicadas por áreas:

Exemplo:

Zona A:  
Corrente de \+2 nós para Norte

Zona B:  
Corrente de \-1 nó para Sul

A implementação inicial não necessita de simulação oceânica completa.

---

# **4\. Sistema de Profundidade**

## **Objetivo**

Controlar áreas navegáveis e criar riscos físicos relacionados ao ambiente marítimo.

A profundidade será um elemento de planejamento de rota, influenciando onde o jogador pode navegar com segurança.

---

## **Funcionamento**

O mundo possuirá um mapa de profundidade:

0-2 metros:  
Área perigosa  
Possibilidade de colisão

2-10 metros:  
Navegação limitada

10+ metros:  
Navegação segura

---

## **Aplicações no Gameplay**

A profundidade influencia:

* Escolha de rotas.  
* Aproximação de ilhas.  
* Entrada em portos.  
* Risco de encalhe.  
* Necessidade de conhecer o ambiente.

---

## **Implementação Inicial**

Na prova de conceito:

* Usar áreas de colisão simples.  
* Criar regiões pré-definidas de perigo.  
* Aplicar penalidades ou bloqueios ao barco.

A implementação futura pode evoluir para:

* Leitura por instrumentos do navio.  
* Informações fornecidas pela tripulação.  
* Cartas náuticas.  
* Observação visual da água.

# **Minigame de Navegação: Sextante**

## **Objetivo**

O sextante é o principal instrumento de navegação do jogador. Ele não funciona como um GPS, mas como uma ferramenta de redução de incerteza. O objetivo é permitir que o jogador descubra sua posição aproximada através de uma medição com margem de erro.

A mecânica deve recompensar a habilidade do jogador, não atributos ou níveis do personagem. A precisão da medição depende exclusivamente da execução do minigame, das condições do ambiente e da qualidade do instrumento.

---

## **Funcionamento do Minigame**

Ao utilizar o sextante, o jogador entra em uma visão de observação.

O objetivo é alinhar corretamente:

* O horizonte do oceano.  
* O astro observado (Sol durante o dia ou estrela durante a noite).

O jogador controla o ajuste do sextante até conseguir o melhor alinhamento possível.

Possíveis fatores que afetam a dificuldade:

* Movimento do navio causado pelas ondas.  
* Tempestades ou clima ruim.  
* Visibilidade reduzida.  
* Distância do horizonte.  
* Qualidade do sextante.

---

## **Resultado da Medição**

O jogador nunca recebe uma posição exata.

A medição retorna uma área provável:

Exemplo:

Latitude: 18°42' S ± 15 km  
Longitude: 38°15' W ± 25 km

No mapa, isso aparece como uma região onde o jogador provavelmente está.

Uma medição ruim gera uma área maior.

Uma medição bem executada gera uma área menor.

---

## **Triangulação Manual**

O jogador pode realizar várias medições ao longo da viagem e comparar os resultados.

Exemplo:

Primeira medição:

Área provável:  
████████████  
████████████  
████████████

Depois de navegar:

Segunda medição:

Área provável:  
     ███████  
     ███████  
     ███████

O cruzamento das duas áreas indica uma região mais provável da posição real.

O jogador pode marcar pontos no mapa, criar anotações e usar referências visuais para melhorar sua navegação.

A ideia é que o conhecimento do jogador substitua um sistema automático de localização.

---

## **Filosofia da Mecânica**

O jogo não deve medir a habilidade do personagem.

Não existirão:

* atributo de navegação;  
* nível de marinheiro;  
* bônus de precisão por experiência.

Um jogador iniciante pode aprender e melhorar praticando.

A progressão acontece através do conhecimento do jogador:

* entender o instrumento;  
* interpretar erros;  
* planejar medições;  
* usar referências;  
* criar suas próprias técnicas de navegação.

O sextante deve ser uma ferramenta que transforma o jogador em um navegador melhor, não um sistema que melhora o personagem.

# **Sistemas de Navegação**

## **Filosofia**

A navegação deve ser um processo de gerenciamento de incerteza e adaptação. O jogador não controla apenas o destino, mas precisa interpretar o ambiente e fazer correções constantes.

O objetivo não é simular perfeitamente a navegação histórica, mas criar uma experiência onde vento, correntes e instrumentos influenciam as decisões do jogador.

---

# **Correntes Marítimas**

## **Objetivo**

As correntes marítimas representam o movimento natural da água e afetam diretamente a trajetória do navio.

Elas possuem dois efeitos principais:

* Alteração da velocidade efetiva da viagem.  
* Desvio gradual da rota planejada.

Uma corrente favorável pode acelerar uma viagem.

Uma corrente contrária pode atrasar o progresso.

Uma corrente lateral pode empurrar o navio para fora da rota sem que o jogador perceba imediatamente.

---

## **Comportamento**

As correntes são sistemas globais e não pertencem a um único chunk.

Cada região do oceano possui características próprias:

* direção;  
* intensidade;  
* variação temporal.

O jogador deve aprender a interpretar mapas, observações e experiência para antecipar essas influências.

---

# **Ventos Dinâmicos**

## **Objetivo**

O vento é o principal elemento de controle da navegação.

O jogador deve ajustar constantemente o rumo para aproveitar melhor a força do vento.

O vento não deve ser completamente parado, pois períodos sem vento criariam uma experiência lenta e pouco interativa.

---

## **Comportamento**

O vento possui:

* direção;  
* intensidade;  
* variação gradual.

Mudanças pequenas e constantes exigem correções frequentes no leme.

O jogador deve encontrar o melhor ângulo entre:

* direção desejada;  
* aproveitamento do vento;  
* segurança da rota.

---

# **Interação entre Sistemas**

A posição final do navio é resultado da combinação:

Movimento do navio  
\+  
Vento  
\+  
Correntes  
\+  
Decisões do jogador

Uma rota planejada raramente será perfeita.

O jogador deve:

* observar o ambiente;  
* corrigir o rumo;  
* realizar medições com o sextante;  
* atualizar sua posição estimada;  
* escolher quando corrigir ou aceitar desvios.

# **Sistema de Descoberta e Atualização de Mapas**

## **Filosofia**

O mapa do jogador representa o conhecimento acumulado da tripulação.

O jogador não deve precisar registrar manualmente todas as informações encontradas durante a viagem. A descoberta acontece naturalmente através da observação e dos instrumentos disponíveis.

A habilidade do jogador está em interpretar informações e tomar decisões, não em realizar tarefas administrativas.

---

# **Atualização Automática do Mapa**

## **Elementos descobertos visualmente**

Quando o jogador observa algo relevante, a informação é adicionada automaticamente ao mapa.

Exemplos:

* Ilhas.  
* Costas.  
* Portos.  
* Pontos de interesse.  
* Perigos de navegação.

Uma ilha desconhecida inicialmente aparece como uma descoberta recente, podendo receber informações adicionais conforme o jogador se aproxima e explora.

---

# **Registro de Correntes Marítimas**

As correntes não precisam ser registradas manualmente.

O sistema utiliza as medições de posição e o histórico de movimento do navio para identificar influências externas.

Exemplo:

1. O jogador define uma rota.  
2. O navio apresenta um desvio constante.  
3. O jogador realiza uma medição com o sextante.  
4. O sistema compara posição esperada e posição real.  
5. Uma corrente é inferida e adicionada ao mapa.

A informação pode aparecer como uma indicação aproximada:

Corrente detectada

Direção: Nordeste  
Intensidade: Moderada  
Confiança: Baixa

Com mais viagens pela região, a informação fica mais precisa.

---

# **Conhecimento como Recurso**

O mapa evolui conforme o jogador navega.

No início:

* grandes áreas desconhecidas;  
* poucas informações;  
* maior incerteza.

Com o tempo:

* correntes conhecidas;  
* rotas eficientes;  
* ilhas catalogadas;  
* melhores previsões de viagem.

O conhecimento do mundo é construído pela experiência do jogador.

# **Obstáculos Marítimos**

## **Filosofia**

O oceano não é apenas espaço vazio entre destinos. A navegação exige atenção ao ambiente e planejamento de rotas.

Obstáculos marítimos não causam fim de jogo, mas criam consequências:

* impedem passagem;  
* reduzem velocidade;  
* causam danos ao navio;  
* obrigam o jogador a encontrar novas rotas.

---

# **Corais**

## **Características**

Recifes de coral são obstáculos permanentes.

Podem existir:

* próximos de ilhas;  
* em regiões tropicais;  
* formando áreas extensas de navegação perigosa.

O jogador pode descobrir e registrar recifes no mapa através da observação ou navegação próxima.

Após descobertos:

Recife conhecido

Área:  
██████

Perigo:  
Alto

---

## **Sinais de Corais**

Antes de uma colisão, o jogador pode identificar sinais visuais e ambientais:

* água com coloração diferente;  
* manchas claras ou azuladas indicando menor profundidade;  
* ondas quebrando em áreas específicas;  
* espuma ou turbulência localizada;  
* presença de aves marinhas;  
* mudança no comportamento das ondas próximas ao obstáculo.

Esses sinais não revelam a localização exata, mas permitem que jogadores atentos reduzam riscos.

---

# **Bancos de Areia**

## **Características**

Bancos de areia representam regiões de pouca profundidade.

Podem ser:

* permanentes;  
* temporários.

Bancos permanentes podem ser adicionados aos mapas após descoberta.

Bancos temporários podem surgir ou desaparecer devido a alterações ambientais.

---

## **Sinais de Bancos de Areia**

Assim como os corais, bancos de areia podem ser identificados antes do contato direto.

Possíveis sinais:

* alteração da cor da água;  
* áreas onde o mar parece mais calmo;  
* ondas quebrando em linhas ou regiões específicas;  
* redução gradual da velocidade do navio;  
* mudança no som das ondas;  
* aves ou animais marinhos concentrados em determinada área.

A intensidade desses sinais depende das condições:

* iluminação;  
* clima;  
* distância;  
* altura das ondas.

---

# **Interação com o Navio**

O contato com obstáculos não destrói automaticamente o navio.

Efeitos possíveis:

* dano ao casco;  
* redução temporária de velocidade;  
* necessidade de reparos;  
* alteração da rota.

A gravidade depende de:

* velocidade do navio;  
* tipo de obstáculo;  
* duração do contato.

---

# **Descoberta**

Obstáculos visíveis são registrados automaticamente no mapa quando observados.

O jogador não precisa fazer anotações manuais.

A informação pode começar como aproximada:

Possível área de baixa profundidade detectada

e evoluir para:

Recife confirmado  
Área mapeada

---

# **Simplificação de Profundidade**

O jogo não simula profundidade real baseada em maré.

A passagem depende apenas da presença de áreas perigosas e das características do navio.

A navegação é baseada em:

* leitura do mapa;  
* observação;  
* planejamento de rota;  
* interpretação dos sinais do ambiente.

# **Instrumentos e Observação Marítima**

## **Filosofia**

A navegação depende da interpretação de informações obtidas através de instrumentos, observação do ambiente e conhecimento da tripulação.

O jogador não recebe todos os dados diretamente em interfaces. Informações importantes são comunicadas por instrumentos ou pela tripulação, mantendo a sensação de estar comandando um navio real.

---

# **Medição de Profundidade**

## **Funcionamento**

O jogador pode solicitar uma medição de profundidade ao tripulante responsável.

A informação é transmitida verbalmente:

Exemplos:

"Profundidade: 40 metros."

"A profundidade está diminuindo rapidamente, capitão."

"Águas rasas à frente."

Não existe um indicador permanente de profundidade na tela.

---

## **Uso na Navegação**

A medição ajuda a identificar:

* aproximação de bancos de areia;  
* regiões perigosas;  
* mudanças inesperadas no relevo submarino.

O jogador deve combinar:

* profundidade medida;  
* aparência da água;  
* comportamento das ondas;  
* mapas existentes.

---

## **Detecção Automática pela Tripulação**

O tripulante pode alertar o jogador quando houver mudanças significativas:

Exemplos:

* queda rápida de profundidade;  
* aumento repentino da profundidade;  
* aproximação de uma área perigosa.

A qualidade da informação depende da situação:

* velocidade do navio;  
* frequência das medições;  
* experiência da tripulação (caso esse sistema seja adicionado futuramente).

---

# **Barômetro e Previsão do Tempo**

## **Funcionamento**

O barômetro fornece uma indicação antecipada de mudanças climáticas.

Ele não informa exatamente o clima futuro, mas indica tendências:

* pressão subindo → tempo tende a estabilizar;  
* pressão caindo → possibilidade de tempestades ou piora do clima.

---

## **Uso pelo Jogador**

O jogador combina:

* leitura do barômetro;  
* aparência das nuvens;  
* cor do céu;  
* comportamento do mar;  
* direção e intensidade do vento.

Exemplo:

Barômetro:

"Pressão caindo."

Ambiente:

* nuvens aumentando;  
* vento mudando;  
* ondas ficando maiores.

Conclusão do jogador:

"Uma tempestade está se aproximando."

---

# **Filosofia de Informação**

O jogo evita transformar sistemas naturais em barras e números constantes.

O jogador aprende a interpretar:

* o mar;  
* o céu;  
* os instrumentos;  
* os avisos da tripulação.

A navegação é baseada em observação e tomada de decisão.

# **Barômetro e Previsão do Tempo**

## **Filosofia**

O clima não deve ser apresentado ao jogador através de indicadores artificiais ou previsões exatas. A mudança do tempo deve ser percebida através de instrumentos, observação do ambiente e conhecimento da tripulação.

O jogador deve interpretar sinais para decidir quando continuar uma viagem, alterar uma rota ou se preparar para condições adversas.

---

# **Funcionamento do Barômetro**

O barômetro é um instrumento de navegação que permite acompanhar alterações na pressão atmosférica.

Ele não prevê o clima diretamente, mas fornece sinais antecipados de mudanças.

O jogador pode consultar o instrumento para observar:

* pressão atual;  
* tendência de subida ou queda;  
* velocidade da mudança.

Exemplo:

Barômetro

Pressão: diminuindo  
Tendência: queda moderada

A informação apresentada deve ser simples e interpretativa, evitando transformar o instrumento em um sistema de previsão exata.

---

# **Interpretação pela Tripulação**

O jogador também pode solicitar a opinião de um tripulante.

O tripulante não fornece apenas o valor do instrumento, mas interpreta a situação considerando outros fatores:

* leitura do barômetro;  
* aparência das nuvens;  
* direção do vento;  
* comportamento do mar;  
* experiência própria.

Exemplos:

"A pressão está caindo, capitão. Eu ficaria atento ao tempo."

"O vento mudou e o ar está pesado. Talvez seja melhor preparar o navio."

A informação fornecida pela tripulação é uma análise, não uma previsão perfeita.

---

# **Observação do Ambiente**

O jogador deve combinar diferentes fontes de informação:

Instrumento:

Pressão diminuindo.

Tripulação:

"O tempo parece querer virar."

Ambiente:

* nuvens aumentando;  
* mudança de cor do céu;  
* ondas mais fortes;  
* alteração do vento.

A decisão final pertence ao jogador.

---

# **Filosofia de Informação**

Cada fonte possui uma função:

| Fonte | Tipo de informação |
| ----- | ----- |
| Barômetro | Dados sobre pressão atmosférica |
| Tripulação | Interpretação dos sinais |
| Ambiente | Confirmação visual das mudanças |

O objetivo não é entregar uma previsão meteorológica precisa, mas criar uma experiência onde o jogador aprenda a reconhecer sinais e tomar decisões com informações incompletas.

# **Instrumentos e Registros de Navegação**

## **Filosofia**

Os instrumentos do navio existem para auxiliar a tomada de decisão do jogador, não para substituir sua percepção.

O jogador não possui um mapa eletrônico ou indicadores constantes de navegação. A informação é obtida através de cartas náuticas, instrumentos físicos, observação do ambiente e relatórios da tripulação.

O conhecimento do mundo é construído durante a viagem.

---

# **Carta Náutica**

## **Funcionamento**

A carta náutica é o principal meio de registro e planejamento.

Ela apresenta:

* regiões conhecidas;  
* ilhas descobertas;  
* perigos de navegação;  
* correntes identificadas;  
* rotas registradas.

A carta não mostra automaticamente a posição exata do navio.

O jogador deve utilizar instrumentos e observação para estimar sua localização e decidir sua rota.

---

## **Atualização da Carta**

Informações observadas pelo jogador são registradas automaticamente.

Exemplos:

* uma ilha avistada pela primeira vez;  
* um recife identificado;  
* uma região de águas rasas;  
* uma corrente detectada através de medições.

O jogador não precisa realizar tarefas administrativas de cartografia.

A habilidade está em interpretar as informações disponíveis.

---

# **Diário de Bordo**

## **Funcionamento**

O diário de bordo registra a história da viagem e o conhecimento acumulado.

Pode conter:

* posições estimadas;  
* medições realizadas;  
* condições climáticas;  
* descobertas;  
* eventos importantes;  
* observações da tripulação.

Exemplo:

Dia 14

Navegando para oeste.  
Corrente forte empurrando para norte.  
Avistada uma ilha desconhecida.  
Tempo piorando ao fim do dia.

O diário funciona como memória da expedição e auxilia no planejamento de viagens futuras.

---

# **Luneta**

## **Funcionamento**

A luneta é uma ferramenta de observação.

Como a percepção do mundo depende principalmente da visão do jogador, a luneta aumenta a capacidade de identificar eventos à distância.

Ela permite:

* observar ilhas antes da aproximação;  
* identificar navios;  
* procurar construções ou portos;  
* reconhecer sinais de fumaça;  
* analisar condições do mar;  
* identificar possíveis perigos.

---

## **Papel na Navegação**

A luneta não revela informações automaticamente.

Ela amplia a capacidade de observação do jogador.

Exemplo:

Sem luneta:

"Parece haver algo no horizonte."

Com luneta:

"É uma embarcação seguindo para sul."

ou:

"Há uma formação de rochas próximas à costa."

A ferramenta recompensa jogadores atentos ao ambiente.

---

# **Sondagem de Profundidade**

## **Funcionamento**

A profundidade é obtida através de uma sondagem manual realizada por um tripulante.

O jogador solicita uma medição e recebe uma resposta verbal.

Exemplos:

"Profundidade: 40 metros."

"A profundidade está diminuindo rapidamente, capitão."

"Águas rasas à frente."

Não existe um indicador permanente de profundidade.

---

## **Uso**

A sondagem auxilia na identificação de:

* bancos de areia;  
* recifes;  
* mudanças no relevo submarino.

O jogador deve combinar:

* profundidade medida;  
* aparência da água;  
* comportamento das ondas;  
* informações da carta náutica.

---

# **Medição de Velocidade**

## **Funcionamento**

O jogador pode solicitar a velocidade atual do navio.

A informação é fornecida pela tripulação.

Exemplos:

"Estamos fazendo 6 nós, capitão."

"Perdemos velocidade com essa corrente."

A velocidade auxilia:

* planejamento de viagens;  
* estimativa de distância percorrida;  
* comparação entre rotas.

---

# **Filosofia de Informação**

Cada ferramenta possui um papel:

| Fonte | Função |
| ----- | ----- |
| Carta náutica | Planejamento e conhecimento acumulado |
| Diário de bordo | Histórico da viagem |
| Luneta | Observação distante |
| Sextante | Estimativa de posição |
| Sondagem | Segurança contra águas rasas |
| Velocidade | Estimativa de deslocamento |
| Tripulação | Interpretação dos dados |

O jogador não recebe uma resposta pronta sobre o mundo. Ele reúne informações, interpreta sinais e toma decisões como comandante do navio.

# **Perigos Marítimos**

## **Filosofia**

O oceano apresenta riscos que exigem atenção constante e planejamento. Nem todos os perigos são eventos extremos; muitos são situações que podem ser evitadas através de observação, instrumentos e conhecimento da região.

Os perigos marítimos não causam fim de jogo, mas podem gerar consequências:

* danos ao navio;  
* perda de tempo;  
* alteração de rotas;  
* necessidade de reparos;  
* risco para a viagem.

---

# **Rochas**

## **Características**

Formações rochosas são obstáculos físicos permanentes que podem existir:

* próximas a ilhas;  
* em regiões costeiras;  
* isoladas em alto-mar.

Diferentemente dos corais e bancos de areia, as rochas podem representar um perigo visual mais evidente, mas ainda podem ser difíceis de identificar em condições ruins.

---

## **Sinais de Rochas**

Antes do contato, o jogador pode perceber:

* ondas quebrando ao redor de uma área específica;  
* espuma ou turbulência localizada;  
* partes da rocha visíveis acima da água;  
* mudança no comportamento das ondas;  
* presença de aves marinhas.

A luneta pode ajudar a identificar formações distantes antes da aproximação.

---

## **Interação com o Navio**

O impacto depende de:

* velocidade do navio;  
* tamanho da formação;  
* ângulo de contato.

Possíveis consequências:

* dano ao casco;  
* perda de velocidade;  
* necessidade de reparo.

---

# **Mar Agitado**

## **Características**

O estado do mar representa as condições das ondas e pode ocorrer mesmo sem uma tempestade.

Mar agitado afeta diretamente a operação do navio.

Possíveis causas:

* mudanças climáticas;  
* ventos fortes;  
* passagem de sistemas meteorológicos;  
* regiões específicas do oceano.

---

## **Efeitos**

O mar agitado pode:

* reduzir a velocidade efetiva;  
* dificultar o controle do navio;  
* aumentar o balanço da embarcação;  
* prejudicar medições com o sextante;  
* dificultar a sondagem de profundidade;  
* reduzir a precisão da observação pela luneta.

O jogador deve decidir se continua a rota, reduz velocidade ou procura uma região mais protegida.

---

# **Destroços**

## **Características**

Destroços representam restos de embarcações ou estruturas perdidas no mar.

Podem ser:

* perigos de navegação;  
* sinais de acontecimentos passados;  
* pontos de descoberta.

Um destroço pode indicar:

* uma região perigosa;  
* uma rota antiga;  
* uma expedição desaparecida;  
* uma história a ser descoberta.

---

## **Interação**

Ao encontrar destroços, o jogador pode:

* observar com a luneta;  
* aproximar-se com cuidado;  
* registrar a localização na carta;  
* investigar futuramente (caso essa mecânica seja adicionada).

Destroços podem funcionar como elementos narrativos e de exploração, não apenas como obstáculos.

---

# **Filosofia de Descoberta**

Os perigos marítimos devem ser identificados através da combinação de:

* observação visual;  
* luneta;  
* cartas náuticas;  
* relatos da tripulação;  
* instrumentos de navegação.

O jogador deve aprender a interpretar sinais antes que o perigo se torne um problema.

# **Sustentação de Expedições**

## **Filosofia**

O jogador possui liberdade para navegar para qualquer direção, mas viagens longas ao desconhecido possuem consequências.

A limitação não é uma barreira artificial nem uma condição de morte. O problema de uma expedição mal planejada é o desgaste progressivo do navio e da tripulação.

Uma viagem bem preparada permite explorar mais longe e retornar em boas condições.

---

# **Abastecimento**

## **Funcionamento**

Antes de uma viagem, o jogador compra suprimentos suficientes para um determinado período de operação.

O sistema não controla itens individuais como comida ou água. O abastecimento representa de forma abstrata:

* alimentação;  
* materiais de manutenção;  
* descanso adequado;  
* organização da tripulação.

Exemplo:

Expedição preparada:

Autonomia estimada:  
30 dias

Se o jogador retornar antes desse período, a viagem ocorre normalmente.

Caso ultrapasse a autonomia planejada, começam as consequências.

---

# **Exaustão da Tripulação**

## **Funcionamento**

A tripulação não morre por falta de suprimentos, mas perde capacidade operacional.

Com o tempo:

* marinheiros ficam menos eficientes;  
* tarefas demoram mais;  
* informações ficam menos confiáveis;  
* algumas funções podem ficar indisponíveis.

Exemplos:

"O responsável pela sondagem precisa descansar, capitão."

"A tripulação está cansada demais para realizar medições frequentes."

A consequência é operacional, não punitiva.

---

# **Desgaste do Navio**

## **Funcionamento**

Viagens prolongadas causam desgaste gradual.

O desgaste representa:

* danos acumulados;  
* necessidade de manutenção;  
* equipamentos degradados;  
* problemas estruturais.

O navio não quebra instantaneamente, mas perde eficiência.

---

# **Efeitos do Desgaste**

O desgaste pode afetar:

## **Navegação**

* sextante menos preciso;  
* barômetro menos confiável;  
* luneta com pior qualidade de observação;  
* estimativa de velocidade menos precisa.

## **Movimento**

* redução da velocidade máxima;  
* pior resposta do navio;  
* maior dificuldade em aproveitar ventos favoráveis.

## **Operação da Tripulação**

* medições mais demoradas;  
* informações menos frequentes;  
* maior chance de erros humanos.

---

# **Retorno de uma Expedição Mal Preparada**

Caso o jogador encontre uma ilha ou porto após ultrapassar a autonomia:

A descoberta não é perdida, mas o jogador chega em condições ruins.

Possíveis consequências:

* navio precisa de reparos;  
* equipamentos precisam ser recuperados;  
* tripulação precisa descansar;  
* será necessário obter recursos antes da próxima viagem.

O local descoberto se torna uma oportunidade de recuperação, não uma punição.

---

# **Filosofia de Progressão**

A progressão acontece através da capacidade de realizar expedições maiores.

O jogador melhora não aumentando atributos, mas:

* aprendendo rotas;  
* conhecendo correntes;  
* preparando melhor viagens;  
* escolhendo quando arriscar;  
* descobrindo locais seguros para reabastecimento.

A pergunta principal deixa de ser:

"Consigo chegar lá?"

e passa a ser:

"Consigo chegar lá e ainda voltar?"

# **Progressão do Navio**

## **Filosofia**

O jogador possui um único navio durante toda a campanha.

A progressão acontece através de:

* melhorias permanentes;  
* manutenção;  
* aquisição de equipamentos;  
* conhecimento acumulado.

O objetivo não é substituir o navio, mas transformar uma embarcação simples em uma ferramenta capaz de realizar grandes expedições.

---

# **Navio Inicial**

## **Estado Inicial**

O navio começa como uma embarcação costeira capaz de pequenas viagens.

Características:

* adequado para navegação próxima à costa;  
* pouca autonomia;  
* instrumentos limitados;  
* poucos recursos de navegação.

O jogador deve inicialmente navegar utilizando principalmente:

* referências visuais;  
* posição de ilhas conhecidas;  
* linha da costa;  
* orientação pelo sol e estrelas.

---

# **Instrumentos**

Cada instrumento possui níveis:

* Ausente  
* Básico  
* Intermediário  
* Avançado

O desgaste pode reduzir temporariamente o nível de alguns equipamentos.

---

# **Sextante**

## **Ausente**

O jogador não possui medição precisa de posição.

Navegação depende de:

* costa;  
* ilhas visíveis;  
* relatos.

---

## **Básico**

Permite medições aproximadas.

Efeitos:

* grande margem de erro;  
* exige boas condições climáticas;  
* medições demoradas.

---

## **Intermediário**

Efeitos:

* menor erro de posição;  
* maior tolerância ao movimento do navio;  
* medições mais rápidas.

---

## **Avançado**

Efeitos:

* alta precisão;  
* menor influência de condições ruins;  
* permite navegação oceânica mais segura.

---

# **Barômetro**

## **Ausente**

O jogador depende apenas:

* nuvens;  
* vento;  
* observação do céu.

---

## **Básico**

Indica mudanças simples:

* pressão subindo;  
* pressão caindo.

---

## **Intermediário**

Permite perceber:

* intensidade da mudança;  
* aproximação de sistemas climáticos.

---

## **Avançado**

Permite análises mais confiáveis:

* mudanças graduais;  
* tendências de longo prazo.

---

# **Luneta**

## **Ausente**

O jogador depende da visão normal.

---

## **Básico**

Permite identificar:

* ilhas próximas;  
* navios;  
* grandes estruturas.

---

## **Intermediário**

Melhora:

* alcance;  
* identificação de detalhes;  
* reconhecimento de perigos.

---

## **Avançado**

Permite:

* observação de grandes distâncias;  
* identificação precisa de sinais;  
* melhor exploração visual.

---

# **Melhorias do Navio**

## **Velas**

### **Básicas**

Configuração inicial.

Consequência:

* menor aproveitamento do vento;  
* menor velocidade.

### **Velas aprimoradas**

Consequência:

* maior velocidade;  
* melhor aproveitamento de ventos favoráveis.

### **Velas avançadas**

Consequência:

* melhor controle;  
* menor perda de velocidade em condições difíceis.

---

# **Casco**

## **Básico**

Navio sofre mais com:

* impactos;  
* mar agitado;  
* desgaste.

## **Reforçado**

Consequência:

* menos dano recebido;  
* maior tolerância a erros.

## **Reforçado para expedições**

Consequência:

* viagens longas com menor manutenção.

---

# **Capacidade de Manutenção**

## **Básica**

Reparos simples.

Consequência:

* desgaste aumenta mais rápido.

## **Melhorada**

Consequência:

* recuperação mais eficiente;  
* menor perda de desempenho.

---

# **Equipamentos de Navegação**

## **Bússola**

Melhorias:

* menor variação;  
* maior estabilidade;  
* melhor leitura em condições difíceis.

---

# **Capacidade de Expedição**

Melhorias podem aumentar:

* duração antes do desgaste;  
* eficiência da tripulação;  
* quantidade de suprimentos carregados.

Isso não representa estoque de itens individuais, mas capacidade operacional.

---

# **Filosofia de Progressão**

No início:

Costa conhecida  
↓  
Ilhas próximas  
↓  
Rotas entre regiões  
↓  
Oceano aberto  
↓  
Grandes expedições

A evolução acontece quando o jogador deixa de depender apenas da visão e passa a confiar em:

* instrumentos;  
* cartas;  
* conhecimento das correntes;  
* planejamento.

# **Estado Inicial do Navio**

## **Filosofia**

O navio inicial é uma embarcação funcional para navegação costeira.

O jogador possui ferramentas suficientes para operar o navio, mas não possui instrumentos avançados para exploração de longas distâncias.

A progressão acontece pela aquisição de conhecimento e equipamentos que reduzem a incerteza.

---

# **Equipamentos Iniciais**

## **Bússola — Básica**

A bússola permite manter direção aproximada.

Uso:

* seguir rumos;  
* retornar para regiões conhecidas;  
* manter uma rota simples.

Limitações:

* não indica posição;  
* não compensa desvios causados por correntes;  
* exige correções frequentes.

---

## **Sondagem de Profundidade — Básica**

A tripulação realiza medições manuais de profundidade.

Uso:

* evitar águas perigosas;  
* identificar aproximação de bancos de areia;  
* navegar próximo à costa.

Limitações:

* medições pontuais;  
* não cria um mapa completo do relevo submarino.

---

## **Medição de Velocidade — Básica**

A tripulação informa a velocidade aproximada do navio.

Uso:

* estimar distância percorrida;  
* planejar duração das viagens;  
* comparar eficiência de rotas.

Limitações:

* possui margem de erro;  
* não considera completamente influência das correntes.

---

# **Equipamentos Não Disponíveis no Início**

## **Sextante**

O jogador não consegue determinar sua posição com precisão.

Consequência:

A navegação depende de:

* referências visuais;  
* costa;  
* ilhas conhecidas;  
* memória do caminho.

---

## **Barômetro**

O jogador não possui previsão antecipada de mudanças climáticas.

Consequência:

Precisa observar:

* nuvens;  
* vento;  
* comportamento do mar.

---

## **Luneta**

A observação é limitada pela visão natural.

Consequência:

* menor alcance de descoberta;  
* maior dificuldade para identificar pontos distantes.

---

# **Progressão Natural**

## **Primeira fase: Navegação costeira**

O jogador aprende:

* controlar o navio;  
* interpretar vento e corrente;  
* usar profundidade;  
* reconhecer perigos.

---

## **Segunda fase: Exploração regional**

Com novas ferramentas:

* luneta;  
* cartas melhores;  
* instrumentos mais precisos.

O jogador começa a explorar além das áreas conhecidas.

---

## **Terceira fase: Navegação oceânica**

Com:

* sextante;  
* barômetro;  
* instrumentos avançados.

O jogador consegue planejar grandes expedições e atravessar regiões desconhecidas.
