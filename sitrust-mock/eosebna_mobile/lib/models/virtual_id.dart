class VirtualId {
  const VirtualId({required this.name, required this.emso});

  final String name;
  final String emso;
}

const mockVirtualId = VirtualId(
  name: String.fromEnvironment('VIRTUAL_ID_NAME', defaultValue: 'Edvin Bečič'),
  emso: String.fromEnvironment('VIRTUAL_ID_EMSO', defaultValue: '2222222222222'),
);
