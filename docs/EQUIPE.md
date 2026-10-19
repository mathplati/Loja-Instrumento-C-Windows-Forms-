# Organização da entrega

## Divisão sugerida

| Responsável a definir | Escopo | Entrega |
| --- | --- | --- |
| Tech Lead | Estrutura, Splash, Menu e integração | Solução compilando e revisão dos PRs |
| Integrante de vendas | Catálogo, carrinho, validação e persistência | Venda concluída e consulta dos dados |
| Integrante de relatório | Filtros, indicadores, detalhes e CSV | Relatório consistente com as vendas |
| Integrante de qualidade | Testes, documentação e identificação | Evidências e integrantes.txt preenchido |

Os nomes e RMs dependem da equipe real. O código inicial não substitui os commits, revisões e PRs que cada integrante deve realizar pelo próprio acesso.

## Branches

`main` contém os três commits iniciais da estrutura. `dev` reúne a implementação em commits pequenos e descritivos. As branches estão locais; o envio ao GitHub não foi feito automaticamente.

Depois de revisar os arquivos, o Tech Lead pode publicar as branches:

```bash
git push -u origin main
git push -u origin dev
```

Para demandas seguintes, cada integrante parte de `dev`:

```bash
git switch dev
git pull --ff-only origin dev
git switch -c feat/nome-da-tarefa
# Desenvolver e registrar no mínimo três commits de trabalho real.
git push -u origin feat/nome-da-tarefa
```

Abrir PR com destino `dev`, explicar o comportamento e as verificações executadas. O Tech Lead revisa, dá feedback e integra. Ao terminar a validação no Windows, abrir PR de `dev` para `main`. Não inventar revisões, aprovações ou commits atribuídos a colegas.

## Antes da entrega

- Preencher `integrantes.txt` com todos os nomes e RMs.
- Confirmar em Settings que o repositório é público e pertence à organização exigida.
- Conceder Write aos integrantes.
- Conferir os PRs para `dev` e o histórico de cada tarefa.
- Executar o roteiro Windows em `docs/VALIDACAO.md`.
- Guardar os dados de demonstração separados dos dados reais.
