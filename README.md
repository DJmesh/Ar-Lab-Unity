# AR Lab - Globo Terrestre Interativo em Realidade Aumentada

Aplicação interativa e pedagógica desenvolvida em Unity 6 com Universal Render Pipeline (URP) para smartphones Android, promovendo o raciocínio espacial e o aprendizado de Geografia na Educação Infantil (faixa etária de 4 a 6 anos) através da manipulação tátil de um globo terrestre tridimensional, reconhecimento gamificado dos 7 continentes, marcadores esféricos de alta fidelidade visual com fauna nativa e telemetria de engajamento assíncrona.

## 🚀 Começando

Projeto desenvolvido como parte dos requisitos práticos e de investigação científica da disciplina **UP801TIN3 - UPX - Realidade Aumentada** do Centro Universitário Facens. O objetivo central é superar os desafios da mediação pedagógica tradicional bidimensional no estágio pré-operatório infantil através de um artefato lúdico em Realidade Aumentada móvel, permitindo a rotação livre tridimensional da Terra, busca orientada por missões e validação em tempo real com reforço positivo visual e correção adaptativa suave.

### 📋 Pré-requisitos

Para compilar, abrir ou executar o projeto, você precisará dos seguintes softwares:

```bash
# 1. Unity Hub instalado com Unity Editor 6000.6.0f1 (Unity 6 LTS)
# 2. Módulos do Unity instalados:
#    - Android Build Support
#    - Android SDK & NDK Tools
#    - OpenJDK (Java 17)
# 3. Git configurado no sistema operacional (Linux/Windows/macOS)
# 4. Dispositivo móvel Android físico:
#    - Android 8.0 Oreo (API 26) ou superior
#    - Suporte a OpenGLES 3.0 ou Vulkan
```

### 🔧 Instalação

* **Clonar o Repositório:**
  ```bash
  git clone git@github.com:DJmesh/Ar-Lab-Unity.git
  ```
* **Abrir no Unity Hub:**
  1. Abra o **Unity Hub**.
  2. Clique em **Add** / **Open** e selecione a pasta raiz clonada `Ar-Lab-Unity`.
  3. Certifique-se de que a versão selecionada seja a **Unity 6000.6.0f1**.
* **Executar no Unity Editor (Simulador Mobile):**
  1. Abra a cena principal em `Assets/Scenes/SampleScene.unity`.
  2. Alterne a aba de visualização para **Simulator** e escolha um perfil de celular (ex: *Google Pixel 7* ou *Samsung Galaxy S22*).
  3. Pressione o botão **Play (▶)** para interagir com o globo via mouse ou touch simulado.
* **Gerar e Instalar a APK no Celular (Android):**
  1. No menu superior da Unity, clique em **AR Lab > 5. Gerar APK para Celular (Android)**.
  2. O script automatizado compilará e salvará o arquivo executável em `Builds/ARLab_GloboTerrestre.apk`.
  3. Transfira o arquivo `.apk` para o seu dispositivo móvel e proceda com a instalação padrão.

## 🛠️ Construído com

* [Unity 6 (6000.6.0f1)](https://unity.com/) - Engine de desenvolvimento de jogos e experiências imersivas
* [Universal Render Pipeline (URP 17.6)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html) - Pipeline gráfico de alto desempenho otimizado para dispositivos móveis
* [Unity New Input System](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest) - Gerenciamento de toques na tela capacitiva, gestos e suporte multiplataforma
* [IL2CPP & ARM64 Architecture](https://docs.unity3d.com/Manual/IL2CPP.html) - Backend de compilação de alta performance para Android
* [C# (.NET Standard 2.1)](https://learn.microsoft.com/dotnet/csharp/) - Linguagem de programação central da lógica e telemetria

## 📌 Versão

* **Versão 1.0** - Estrutura inicial do globo 3D, texturas planetárias e controle tátil de rotação física com inércia amortecida.
* **Versão 1.1** - *Refatoração e Gamificação Pedagógica* - *data 07/10/26*:
  * Mapeamento trigonométrico e geográfico exato dos 7 continentes (América do Sul, África, Europa, Ásia, Oceania centrada na Austrália, América do Norte e Antártida).
  * Marcadores 3D esféricos com cores distintas, pulsação contínua e associação com fauna nativa.
  * Sistema de missões com score oficial "Acertos: X de 7".
  * Feedback visual duplo: efeito de piscar em Verde no acerto e rotação automática suave corretiva da Terra com piscar em Vermelho no erro.
  * Módulo de telemetria científica assíncrona (`TelemetryLogger`) para coleta de métricas de aprendizagem.
  * Pipeline automatizado de compilação da APK com mitigação de tela preta e estabilidade gráfica para Android.

## ✒️ Autores

* **Eduardo Augusto Prestes Júnior** - *Trabalho Inicial & Desenvolvimento de Software* - [UP801TIN3 - UPX - Realidade Aumentada](https://facens.instructure.com/courses/18484)
* **Eduardo Cunha Soares** - *Desenvolvimento, Integração & Pesquisa* - [UP801TIN3 - UPX - Realidade Aumentada](https://facens.instructure.com/courses/18484)
* **Leonardo de Arruda Macedo** - *Modelagem 3D, Texturização & UX Pedagógica* - [UP801TIN3 - UPX - Realidade Aumentada](https://facens.instructure.com/courses/18484)
* **Matheus de Souza** - *Desenvolvimento de Mecânicas & Telemetria Científica* - [UP801TIN3 - UPX - Realidade Aumentada](https://facens.instructure.com/courses/18484)
* **Guilherme Augusto Ferreira** - *Testes de Validação, QA & Documentação Técnica* - [UP801TIN3 - UPX - Realidade Aumentada](https://facens.instructure.com/courses/18484)
