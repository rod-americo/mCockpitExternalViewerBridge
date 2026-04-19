# mCockpitExternalViewerBridge

Bridge entre o `mCockpit` e visualizadores DICOM externos, como `OsiriX` e `RadiAnt`.

O objetivo deste projeto é remover atrito no fluxo de abertura de exames quando o `mCockpit` precisa delegar a visualização para um viewer externo. Em vez de depender de uma integração rígida ou de ajustes manuais no uso diário, o bridge intercepta a chamada do sistema e redireciona o exame para o viewer configurado.

## O problema que este projeto resolve

Em ambientes reais, o problema raramente é apenas “abrir um viewer”.

O problema é garantir que:

- o `mCockpit` consiga chamar um executável compatível com o plugin em uso;
- os argumentos recebidos sejam interpretados corretamente;
- o exame seja aberto no viewer certo;
- o fluxo continue utilizável sem exigir adaptação manual da operação.

Na prática, este projeto existe para fazer a integração entre o `mCockpit` e viewers externos funcionar de forma previsível.

## Como funciona

1. O `mCockpit` chama o executável configurado no plugin, tipicamente no lugar do `ispilot.exe`.
2. O bridge recebe os argumentos da chamada.
3. O arquivo `config.ini` define qual viewer deve ser usado.
4. Se o viewer configurado for `RadiAnt`, o bridge abre o executável do RadiAnt apontando para a pasta do estudo.
5. Se o viewer configurado for `OsiriX` ou `Horos`, o bridge dispara a URL `osirix://` usando o `Accession Number`.

## Estrutura do projeto

- `proxy.cs`: código-fonte C# do bridge
- `config.ini`: configuração local do viewer e dos caminhos necessários

## Requisitos

- Windows no ambiente em que o `mCockpit` estiver rodando
- .NET Framework com `csc.exe` disponível para compilação
- viewer externo instalado, como `RadiAnt` ou `OsiriX/Horos`
- configuração correta do plugin do `mCockpit`

## Compilação

No Windows em que o `mCockpit` está rodando, compile o código com:

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:winexe /out:ispilot.exe proxy.cs
```

Isso gera um executável `ispilot.exe` em modo silencioso, sem janela de console.

## Configuração do plugin do mCockpit

Certifique-se de que o arquivo de configuração do plugin (`Plugin\mIntegrador.Plugin.Vida.dll.config`) esteja ajustado para enviar os argumentos esperados pelo bridge.

A chave `UsarUrlEspecifica` deve estar como `true`:

```xml
<configuration>
  <add key="UsarUrlEspecifica" value="true" />
  <add key="UrlEspecifica" value="..." />
</configuration>
```

Esse ajuste é importante para que o `mCockpit` envie o argumento `-qr`, que contém o `Accession Number` usado pelo bridge.

## Configuração local

Crie ou edite o arquivo `config.ini` na mesma pasta do executável:

```ini
[General]
viewer=radiant
; viewer=osirix

[RadiAnt]
radiant_exe=C:\Program Files\RadiAntViewer\RadiAntViewer.exe
radiant_dicom=C:\DICOM
```

### Parâmetros principais

- `viewer`: define o viewer a ser usado (`radiant`, `osirix` ou `horos`)
- `radiant_exe`: caminho do executável do RadiAnt
- `radiant_dicom`: diretório base em que os exames ficam armazenados para abertura pelo RadiAnt

## Deploy

1. Defina a pasta em que o plugin do `mCockpit` passará a procurar o executável de abertura do viewer externo.
2. Crie essa pasta manualmente, se ela ainda não existir.
3. Copie para essa pasta:
   - `ispilot.exe`
   - `config.ini`

Dependendo da instalação, isso pode exigir privilégios de administrador.

### Onde o `ispilot.exe` deve ser colocado

Na prática, o `ispilot.exe` compilado deve ficar no local em que o plugin do `mCockpit` foi configurado para procurar o executável de abertura do viewer externo.

Neste projeto, o cenário correto não é substituir um `ispilot.exe` já existente.

O esperado é:

- definir um caminho próprio para o bridge;
- criar a árvore de diretório correspondente;
- colocar nesse local o `ispilot.exe` compilado e o `config.ini`;
- apontar o plugin do `mCockpit` para esse caminho.

Em instalações parecidas com as já observadas, isso costuma apontar para algo como:

```text
C:\Program Files\Intrasense\Myrian\
```

mas esse valor deve ser tratado apenas como exemplo.

O ponto principal é: o caminho real precisa ser o mesmo configurado no plugin do `mCockpit`, mesmo que a pasta precise ser criada do zero.

Regra prática:

- não presumir que já exista um `ispilot.exe` no ambiente;
- criar a pasta de destino quando necessário;
- colocar `ispilot.exe` e `config.ini` exatamente no caminho configurado no plugin;
- validar a abertura do viewer usando esse caminho como origem da integração.

## Comportamento por viewer

### RadiAnt

O bridge:

- lê o `Accession Number` recebido;
- monta o caminho do estudo dentro do diretório configurado em `radiant_dicom`;
- chama o `RadiAntViewer.exe` com o argumento apropriado.

Se o executável do RadiAnt não existir ou a abertura falhar, o bridge tenta abrir a pasta do estudo no Explorer como fallback.

### OsiriX / Horos

O bridge:

- lê o `Accession Number`;
- monta a URL `osirix://?methodName=displayStudy&AccessionNumber=...`;
- dispara essa URL no sistema.

## Limitações atuais

- o parser de `config.ini` é propositalmente simples;
- o projeto assume convenções locais de diretório para o RadiAnt;
- a estratégia de logging está desativada no código atual.

## Histórico resumido

1. Investigação do comportamento do plugin e identificação da chamada ao `ispilot.exe`
2. Ajuste de configuração no `mCockpit` para receber o argumento `-qr`
3. Primeira integração com `OsiriX`
4. Evolução para uma versão unificada com `config.ini` e suporte ao `RadiAnt`

## Uso pretendido

Este projeto existe para resolver um problema específico de integração: permitir que o `mCockpit` abra exames em viewers externos não contemplados pela própria aplicação, de forma previsível e com menos atrito no fluxo real.
