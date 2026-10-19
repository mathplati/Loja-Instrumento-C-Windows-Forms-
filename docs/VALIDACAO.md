# Evidências e roteiro de validação

## Executado neste ambiente Linux

- Build Release da solução com avisos tratados como erros.
- 19 verificações do domínio: catálogo, valores decimais, desconto, arredondamento, validação, agrupamento de linhas, persistência, concorrência entre chamadas e instâncias no mesmo processo, datas, pagamento, indicadores, CSV e proteção de arquivos corrompidos ou com registros incompletos.
- Fluxo automatizado no Chromium: abertura, menu vazio, busca por marca, categoria, busca vazia, carrinho, alterações de quantidade, desconto, venda, comprovante, filtros, detalhes, CSV baixado, recarga, indicadores, preservação do pedido ao navegar, confirmação para limpar, rejeição de entradas inválidas na API e layout de 390 px sem rolagem horizontal.
- Reinício do serviço com leitura da venda anterior, comparando todos os campos.
- Capturas em `artifacts/screenshots`, usando **Cliente demonstração** em um arquivo temporário. Os testes não inserem vendas na pasta de dados real da loja.

As capturas são da interface web de apoio. O Windows Forms foi compilado para Windows; sua execução e o designer não estão disponíveis neste Linux.

## Verificações nativas aprovadas no Windows

O projeto `tests/Sonora.Desktop.Checks` contém 17 verificações nativas: Splash e sinal de transição, menu, navegação, carrinho, total com desconto, preservação do pedido, busca/categoria, persistência, relatório, filtros e acesso aos controles em dois tamanhos de janela. Os fluxos adicionais acionam a finalização real e a confirmação, abrem e fecham os detalhes, salvam e cancelam a exportação pelo seletor nativo e validam a quantidade ainda em edição ao usar `Ctrl+Enter`. Os dados são temporários, com capturas dos formulários e dos diálogos.

O workflow `.github/workflows/windows-desktop.yml` foi executado em `windows-latest` e passou no commit `2bba2bc`: build, verificador de domínio, verificador nativo, captura e geração do pacote. A [execução 39341962230](https://github.com/mathplati/Loja-Instrumento-C-Windows-Forms-/actions/runs/39341962230) confirma a conclusão de todas as etapas com sucesso. Os artefatos são `Sonora-telas-Windows-Forms` (PNGs nativos) e `Sonora-Windows-x64` (aplicativo).

A execução anterior acima aprovou os 13 checks anteriores. O workflow `finish-delivery.yml` executa os 17 checks atuais e grava a saída completa no resumo do job Windows. Consulte essa execução para o resultado dos novos diálogos; as capturas incluem confirmação de venda, detalhes, seletor CSV e confirmação da exportação. A revisão visual humana, o designer e a escala de 150% continuam no roteiro manual.

## Roteiro obrigatório em Windows

1. Instalar o .NET Desktop Runtime 8 x64 (ou Visual Studio 2022 atualizado com desenvolvimento desktop .NET).
2. Abrir `Sonora.sln` e definir `Sonora.Desktop` como projeto de inicialização, ou executar o pacote em `artifacts/windows`.
3. Confirmar Splash e transição automática para Menu.
4. Abrir Nova venda, filtrar por categoria e pesquisar por marca.
5. Adicionar guitarra e violão, aumentar/diminuir a quantidade e confirmar o total.
6. Informar cliente, Pix e 10% de desconto. Com uma guitarra e um violão, o total deve ser **R$ 2.150,10**.
7. Finalizar, verificar a confirmação e o carrinho limpo.
8. Abrir Relatório. Conferir cliente, itens, pagamento e total; abrir detalhes com dois cliques.
9. Filtrar por Dinheiro: a venda Pix deve desaparecer. Restaurar todos os pagamentos.
10. Exportar CSV, abrir em planilha e conferir acentos, campos e valores.
11. Fechar e reabrir o aplicativo: a venda deve continuar no relatório.
12. Verificar redimensionamento, escala de 100%/150%, navegação por Tab e o botão de limpar pedido.

O roteiro manual completo acima continua pendente. A aprovação automática cobre especificamente os comportamentos descritos na seção de verificações nativas.
