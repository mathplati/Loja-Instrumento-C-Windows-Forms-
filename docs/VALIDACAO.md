# Evidências e roteiro de validação

## Executado neste ambiente Linux

- Build Release da solução com avisos tratados como erros.
- 18 verificações do domínio: catálogo, valores decimais, desconto, arredondamento, validação, agrupamento de linhas, persistência, concorrência no mesmo processo, datas, pagamento, indicadores, CSV e proteção de arquivos corrompidos ou com registros incompletos.
- Fluxo automatizado no Chromium: abertura, menu vazio, busca por marca, categoria, busca vazia, carrinho, alterações de quantidade, desconto, venda, comprovante, filtros, detalhes, CSV baixado, recarga, indicadores, preservação do pedido ao navegar, confirmação para limpar, rejeição de entradas inválidas na API e layout de 390 px sem rolagem horizontal.
- Reinício do serviço com leitura da venda anterior, comparando todos os campos.
- Capturas em `artifacts/screenshots`, usando **Cliente demonstração** em um arquivo temporário. Os testes não inserem vendas na pasta de dados real da loja.

As capturas são da interface web de apoio. O Windows Forms foi compilado para Windows; sua execução e o designer não estão disponíveis neste Linux.

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

Os passos acima ainda dependem de execução em Windows; não são apresentados como aprovados.
